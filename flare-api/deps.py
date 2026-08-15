import asyncio
import os
from collections.abc import Callable
from typing import Annotated, TypeVar

from fastapi import Depends, HTTPException, Request
from nvflare.fuel.flare_api.flare_api import Session

# Auth to the FLARE server is mTLS: client.crt/client.key/rootCA.pem from a
# provisioned admin startup kit (see flare/submit.sh), mounted read-only into
# this container. FLARE_ADMIN_USER is just an identity label, not a secret -
# the cert is what the server actually trusts.
FLARE_ADMIN_USER = os.environ.get("FLARE_ADMIN_USER", "admin@flecke.xyz")
FLARE_STARTUP_KIT_DIR = os.environ.get("FLARE_STARTUP_KIT_DIR", "/admin")
# directory of built job definitions, e.g. flare/jobs mounted in (see submit.sh)
FLARE_JOBS_DIR = os.environ.get("FLARE_JOBS_DIR", "/jobs")
# flare/project.yml and flare/workspace, mounted read-write (see provision.py
# and add-client.sh, which routes/clients.py's provision_client mirrors the
# registration half of)
FLARE_PROJECT_YML = os.environ.get("FLARE_PROJECT_YML", "/flare-project/project.yml")
FLARE_WORKSPACE_DIR = os.environ.get("FLARE_WORKSPACE_DIR", "/flare-project/workspace")

T = TypeVar("T")


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
