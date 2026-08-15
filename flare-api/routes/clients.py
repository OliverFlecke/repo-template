import asyncio
import os
import re
import subprocess
import sys
import zipfile
from io import BytesIO

import yaml
from fastapi import APIRouter, HTTPException, Request, Response
from pydantic import BaseModel

from deps import FLARE_PROJECT_YML, FLARE_WORKSPACE_DIR, SessionDep, flare_call

router = APIRouter()

CLIENT_NAME_RE = re.compile(r"^[A-Za-z0-9_.-]+$")


class ClientStatus(BaseModel):
    name: str
    org: str | None = None
    connected: bool
    last_connect_time: float | None = None


class _IndentedYamlDumper(yaml.SafeDumper):
    # PyYAML's default dumper doesn't indent list items under their parent
    # key, which rewrites project.yml's whole formatting on every save.
    def increase_indent(self, flow=False, indentless=False):
        return super().increase_indent(flow, False)


@router.get("/clients", operation_id="listClients")
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
                arcname = os.path.join(client_name, os.path.relpath(full_path, kit_dir))
                zf.write(full_path, arcname)
    return buffer.getvalue()


@router.post(
    "/clients/{name}",
    operation_id="provisionClient",
    response_class=Response,
    responses={
        200: {
            "description": "Startup kit for the newly provisioned client, as a zip file.",
            "content": {
                "application/zip": {"schema": {"type": "string", "format": "binary"}}
            },
        }
    },
)
async def provision_client(
    name: str, request: Request, org: str | None = None
) -> Response:
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
        insert_at = next(i for i, p in enumerate(participants) if p["type"] == "admin")
        participants.insert(
            insert_at, {"name": name, "type": "client", "org": client_org}
        )
        with open(FLARE_PROJECT_YML, "w") as f:
            yaml.dump(
                project,
                f,
                Dumper=_IndentedYamlDumper,
                sort_keys=False,
                default_flow_style=False,
            )

        try:
            await asyncio.to_thread(_run_provision)
            zip_bytes = await asyncio.to_thread(_zip_startup_kit, project["name"], name)
        except RuntimeError as e:
            raise HTTPException(
                status_code=502, detail=f"provisioning failed: {e}"
            ) from e

    return Response(
        content=zip_bytes,
        media_type="application/zip",
        headers={"Content-Disposition": f'attachment; filename="{name}.zip"'},
    )
