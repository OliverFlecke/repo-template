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


@app.get("/health")
async def health(request: Request, session: Annotated[Session, Depends(get_session)]):
    try:
        return await run_command(request, session.get_system_info)
    except Exception as e:
        raise HTTPException(
            status_code=502, detail=f"flare server unreachable: {e}"
        ) from e