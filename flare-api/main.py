import asyncio
from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

import httpx
from fastapi import Depends, FastAPI
from fastapi.middleware.cors import CORSMiddleware
from nvflare.fuel.flare_api.flare_api import new_secure_session

from auth import require_system_admin
from deps import FLARE_ADMIN_USER, FLARE_STARTUP_KIT_DIR
from routes import clients, health, jobs


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

app.include_router(health.router)
app.include_router(clients.router)
app.include_router(jobs.router)
