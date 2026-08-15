import os

from fastapi import APIRouter, HTTPException, Request
from pydantic import BaseModel

from deps import FLARE_JOBS_DIR, SessionDep, flare_call

router = APIRouter()


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


@router.get("/job-definitions", operation_id="listJobDefinitions")
async def list_job_definitions() -> list[str]:
    if not os.path.isdir(FLARE_JOBS_DIR):
        return []
    return sorted(
        name
        for name in os.listdir(FLARE_JOBS_DIR)
        if not name.startswith(".")
        and os.path.isdir(os.path.join(FLARE_JOBS_DIR, name))
    )


@router.get("/jobs", operation_id="listJobs")
async def list_jobs(request: Request, session: SessionDep) -> JobsResponse:
    jobs = await flare_call(
        request, lambda: session.list_jobs(detailed=True, reverse=True)
    )
    parsed = [Job.model_validate(j) for j in jobs]
    active = [j for j in parsed if not (j.status or "").startswith("FINISHED")]
    completed = [j for j in parsed if (j.status or "").startswith("FINISHED")]
    return JobsResponse(active=active, completed=completed)


@router.post("/jobs/{job_name}", operation_id="submitJob")
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
