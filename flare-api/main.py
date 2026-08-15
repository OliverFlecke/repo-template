import asyncio
import os
from collections.abc import AsyncIterator, Callable
from contextlib import asynccontextmanager
from typing import Annotated, TypeVar

from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from nvflare.fuel.flare_api.flare_api import Session, new_secure_session
from pydantic import BaseModel

# Auth to the FLARE server is mTLS: client.crt/client.key/rootCA.pem from a
# provisioned admin startup kit (see flare/submit.sh), mounted read-only into
# this container. FLARE_ADMIN_USER is just an identity label, not a secret -
# the cert is what the server actually trusts.
FLARE_ADMIN_USER = os.environ.get("FLARE_ADMIN_USER", "admin@flecke.xyz")
FLARE_STARTUP_KIT_DIR = os.environ.get("FLARE_STARTUP_KIT_DIR", "/admin")
# directory of built job definitions, e.g. flare/jobs mounted in (see submit.sh)
FLARE_JOBS_DIR = os.environ.get("FLARE_JOBS_DIR", "/jobs")

T = TypeVar("T")


class ServerInfo(BaseModel):
    status: str | None
    start_time: float | None


class ClientInfo(BaseModel):
    name: str
    last_connect_time: float | None


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
    try:
        yield
    finally:
        await asyncio.to_thread(session.close)


app = FastAPI(lifespan=lifespan)

# No auth of its own yet (frontend auth for this API is TODO) and no cookies
# are involved, so a wide-open CORS policy is fine for now.
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
async def list_clients(request: Request, session: SessionDep) -> list[ClientInfo]:
    info = await flare_call(request, session.get_system_info)
    return [
        ClientInfo(name=c.name, last_connect_time=c.last_connect_time)
        for c in info.client_info
    ]


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
    jobs = await flare_call(request, lambda: session.list_jobs(detailed=True))
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
