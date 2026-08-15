import asyncio
import os
import re
import subprocess
import sys
import zipfile
from collections.abc import AsyncIterator, Callable
from contextlib import asynccontextmanager
from io import BytesIO
from typing import Annotated, TypeVar

import httpx
import yaml
from fastapi import Depends, FastAPI, HTTPException, Request, Response
from fastapi.middleware.cors import CORSMiddleware
from nvflare.fuel.flare_api.flare_api import Session, new_secure_session
from pydantic import BaseModel

from auth import require_system_admin

# Auth to the FLARE server is mTLS: client.crt/client.key/rootCA.pem from a
# provisioned admin startup kit (see flare/submit.sh), mounted read-only into
# this container. FLARE_ADMIN_USER is just an identity label, not a secret -
# the cert is what the server actually trusts.
FLARE_ADMIN_USER = os.environ.get("FLARE_ADMIN_USER", "admin@flecke.xyz")
FLARE_STARTUP_KIT_DIR = os.environ.get("FLARE_STARTUP_KIT_DIR", "/admin")
# directory of built job definitions, e.g. flare/jobs mounted in (see submit.sh)
FLARE_JOBS_DIR = os.environ.get("FLARE_JOBS_DIR", "/jobs")
# flare/project.yml and flare/workspace, mounted read-write (see provision.py
# and add-client.sh, which this endpoint mirrors the registration half of)
FLARE_PROJECT_YML = os.environ.get("FLARE_PROJECT_YML", "/flare-project/project.yml")
FLARE_WORKSPACE_DIR = os.environ.get(
    "FLARE_WORKSPACE_DIR", "/flare-project/workspace"
)

CLIENT_NAME_RE = re.compile(r"^[A-Za-z0-9_.-]+$")

T = TypeVar("T")


class _IndentedYamlDumper(yaml.SafeDumper):
    # PyYAML's default dumper doesn't indent list items under their parent
    # key, which rewrites project.yml's whole formatting on every save.
    def increase_indent(self, flow=False, indentless=False):
        return super().increase_indent(flow, False)


class ServerInfo(BaseModel):
    status: str | None
    start_time: float | None


class ClientInfo(BaseModel):
    name: str
    last_connect_time: float | None


class ClientStatus(BaseModel):
    name: str
    org: str | None = None
    connected: bool
    last_connect_time: float | None = None


class JobSummary(BaseModel):
    job_id: str
    app_name: str | None = None


class HealthResponse(BaseModel):
    server_info: ServerInfo
    client_info: list[ClientInfo]
    job_info: list[JobSummary]


class Job(BaseModel):
    job_id: str
    name: str | None = None
    status: str | None = None
    submitter_name: str | None = None
    submit_time_iso: str | None = None
    duration: str | None = None


class JobsResponse(BaseModel):
    active: list[Job]
    completed: list[Job]


class SubmitJobResponse(BaseModel):
    job_id: str


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    session = await asyncio.to_thread(
        new_secure_session, FLARE_ADMIN_USER, FLARE_STARTUP_KIT_DIR
    )
    app.state.flare_session = session
    # the session holds one persistent socket to the server's admin port -
    # commands must be serialized, not run concurrently across requests.
    app.state.flare_lock = asyncio.Lock()
    # project.yml/workspace are mutated by provision_client - one at a time.
    app.state.provision_lock = asyncio.Lock()
    async with httpx.AsyncClient() as http_client:
        app.state.http_client = http_client
        try:
            yield
        finally:
            await asyncio.to_thread(session.close)


app = FastAPI(lifespan=lifespan, dependencies=[Depends(require_system_admin)])

# Every endpoint requires a valid bearer token for a user who is an admin on
# system:core in OpenFGA (see auth.py) - a browser origin can't forge that,
# so CORS itself can stay wide open.
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


def get_session(request: Request) -> Session:
    return request.app.state.flare_session


async def run_command(request: Request, fn: Callable[[], T]) -> T:
    async with request.app.state.flare_lock:
        return await asyncio.to_thread(fn)


async def flare_call(request: Request, fn: Callable[[], T]) -> T:
    try:
        return await run_command(request, fn)
    except Exception as e:
        raise HTTPException(status_code=502, detail=f"flare server error: {e}") from e


SessionDep = Annotated[Session, Depends(get_session)]


@app.get("/health", operation_id="getHealth")
async def health(request: Request, session: SessionDep) -> HealthResponse:
    info = await flare_call(request, session.get_system_info)
    return HealthResponse(
        server_info=ServerInfo(
            status=info.server_info.status, start_time=info.server_info.start_time
        ),
        client_info=[
            ClientInfo(name=c.name, last_connect_time=c.last_connect_time)
            for c in info.client_info
        ],
        job_info=[
            JobSummary(job_id=j.job_id, app_name=j.app_name) for j in info.job_info
        ],
    )


@app.get("/clients", operation_id="listClients")
async def list_clients(request: Request, session: SessionDep) -> list[ClientStatus]:
    info = await flare_call(request, session.get_system_info)
    connected = {c.name: c.last_connect_time for c in info.client_info}

    with open(FLARE_PROJECT_YML) as f:
        project = yaml.safe_load(f)
    provisioned = {
        p["name"]: p.get("org")
        for p in project["participants"]
        if p["type"] == "client"
    }

    # union, not just provisioned - a client could be connected via a cert
    # issued outside project.yml (the server trusts any cert signed by the
    # root CA, see flare/README.md), so don't hide it just because of that.
    names = sorted(set(provisioned) | set(connected))
    return [
        ClientStatus(
            name=name,
            org=provisioned.get(name),
            connected=name in connected,
            last_connect_time=connected.get(name),
        )
        for name in names
    ]


def _run_provision() -> None:
    # flare-api depends on nvflare too, so its own venv has the CLI - call it
    # by path since the container doesn't put .venv/bin on PATH (see Dockerfile).
    nvflare_bin = os.path.join(os.path.dirname(sys.executable), "nvflare")
    result = subprocess.run(
        [
            nvflare_bin,
            "provision",
            "-p",
            FLARE_PROJECT_YML,
            "-w",
            FLARE_WORKSPACE_DIR,
            "--force",
        ],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr or result.stdout)


def _zip_startup_kit(project_name: str, client_name: str) -> bytes:
    project_dir = os.path.join(FLARE_WORKSPACE_DIR, project_name)
    prod_dirs = sorted(d for d in os.listdir(project_dir) if d.startswith("prod_"))
    if not prod_dirs:
        raise RuntimeError("provisioning produced no output")
    kit_dir = os.path.join(project_dir, prod_dirs[-1], client_name)
    if not os.path.isdir(kit_dir):
        raise RuntimeError(f"no startup kit produced for '{client_name}'")

    buffer = BytesIO()
    with zipfile.ZipFile(buffer, "w", zipfile.ZIP_DEFLATED) as zf:
        for root, _, files in os.walk(kit_dir):
            for file in files:
                full_path = os.path.join(root, file)
                arcname = os.path.join(
                    client_name, os.path.relpath(full_path, kit_dir)
                )
                zf.write(full_path, arcname)
    return buffer.getvalue()


@app.post(
    "/clients/{name}",
    operation_id="provisionClient",
    response_class=Response,
    responses={
        200: {
            "description": "Startup kit for the newly provisioned client, as a zip file.",
            "content": {"application/zip": {"schema": {"type": "string", "format": "binary"}}},
        }
    },
)
async def provision_client(name: str, request: Request, org: str | None = None) -> Response:
    if not CLIENT_NAME_RE.fullmatch(name):
        raise HTTPException(status_code=400, detail="invalid client name")

    async with request.app.state.provision_lock:
        with open(FLARE_PROJECT_YML) as f:
            project = yaml.safe_load(f)
        participants = project["participants"]
        if any(p["name"] == name for p in participants):
            raise HTTPException(
                status_code=409, detail=f"client '{name}' already exists"
            )

        client_org = org or next(
            p["org"] for p in participants if p["type"] == "client"
        )
        insert_at = next(
            i for i, p in enumerate(participants) if p["type"] == "admin"
        )
        participants.insert(
            insert_at, {"name": name, "type": "client", "org": client_org}
        )
        with open(FLARE_PROJECT_YML, "w") as f:
            yaml.dump(
                project, f, Dumper=_IndentedYamlDumper, sort_keys=False, default_flow_style=False
            )

        try:
            await asyncio.to_thread(_run_provision)
            zip_bytes = await asyncio.to_thread(
                _zip_startup_kit, project["name"], name
            )
        except RuntimeError as e:
            raise HTTPException(
                status_code=502, detail=f"provisioning failed: {e}"
            ) from e

    return Response(
        content=zip_bytes,
        media_type="application/zip",
        headers={"Content-Disposition": f'attachment; filename="{name}.zip"'},
    )


@app.get("/job-definitions", operation_id="listJobDefinitions")
async def list_job_definitions() -> list[str]:
    if not os.path.isdir(FLARE_JOBS_DIR):
        return []
    return sorted(
        name
        for name in os.listdir(FLARE_JOBS_DIR)
        if not name.startswith(".")
        and os.path.isdir(os.path.join(FLARE_JOBS_DIR, name))
    )


@app.get("/jobs", operation_id="listJobs")
async def list_jobs(request: Request, session: SessionDep) -> JobsResponse:
    jobs = await flare_call(
        request, lambda: session.list_jobs(detailed=True, reverse=True)
    )
    parsed = [Job.model_validate(j) for j in jobs]
    active = [j for j in parsed if not (j.status or "").startswith("FINISHED")]
    completed = [j for j in parsed if (j.status or "").startswith("FINISHED")]
    return JobsResponse(active=active, completed=completed)


@app.post("/jobs/{job_name}", operation_id="submitJob")
async def submit_job(
    job_name: str, request: Request, session: SessionDep
) -> SubmitJobResponse:
    if job_name in (".", ".."):
        raise HTTPException(status_code=400, detail="invalid job name")
    job_path = os.path.join(FLARE_JOBS_DIR, job_name)
    if not os.path.isdir(job_path):
        raise HTTPException(
            status_code=404, detail=f"no job definition found for '{job_name}'"
        )
    job_id = await flare_call(request, lambda: session.submit_job(job_path))
    return SubmitJobResponse(job_id=job_id)
