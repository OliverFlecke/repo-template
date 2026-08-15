import asyncio
import os
from collections.abc import AsyncIterator, Callable
from contextlib import asynccontextmanager
from typing import Annotated, TypeVar

from fastapi import Depends, FastAPI, HTTPException, Request
from nvflare.fuel.flare_api.flare_api import Session, new_secure_session

# Auth to the FLARE server is mTLS: client.crt/client.key/rootCA.pem from a
# provisioned admin startup kit (see flare/submit.sh), mounted read-only into
# this container. FLARE_ADMIN_USER is just an identity label, not a secret -
# the cert is what the server actually trusts.
FLARE_ADMIN_USER = os.environ.get("FLARE_ADMIN_USER", "admin@flecke.xyz")
FLARE_STARTUP_KIT_DIR = os.environ.get("FLARE_STARTUP_KIT_DIR", "/admin")
# directory of built job definitions, e.g. flare/jobs mounted in (see submit.sh)
FLARE_JOBS_DIR = os.environ.get("FLARE_JOBS_DIR", "/jobs")

T = TypeVar("T")


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


@app.get("/health")
async def health(request: Request, session: SessionDep):
    return await flare_call(request, session.get_system_info)


@app.get("/clients")
async def list_clients(request: Request, session: SessionDep):
    info = await flare_call(request, session.get_system_info)
    return info.client_info


@app.get("/jobs")
async def list_jobs(request: Request, session: SessionDep):
    jobs = await flare_call(request, lambda: session.list_jobs(detailed=True))
    active = [j for j in jobs if not j.get("status", "").startswith("FINISHED")]
    completed = [j for j in jobs if j.get("status", "").startswith("FINISHED")]
    return {"active": active, "completed": completed}


@app.post("/jobs/{job_name}")
async def submit_job(job_name: str, request: Request, session: SessionDep):
    if job_name in (".", ".."):
        raise HTTPException(status_code=400, detail="invalid job name")
    job_path = os.path.join(FLARE_JOBS_DIR, job_name)
    if not os.path.isdir(job_path):
        raise HTTPException(status_code=404, detail=f"no job definition found for '{job_name}'")
    job_id = await flare_call(request, lambda: session.submit_job(job_path))
    return {"job_id": job_id}