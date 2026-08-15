from fastapi import APIRouter, Request
from pydantic import BaseModel

from deps import SessionDep, flare_call

router = APIRouter()


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


@router.get("/health", operation_id="getHealth")
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
