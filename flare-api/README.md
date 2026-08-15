# flare-api

FastAPI wrapper around the [FLARE](../flare) server's admin API.

Auth to the FLARE server is mTLS via a provisioned admin startup kit (see
`flare/submit.sh`) — mount it read-only and point `FLARE_STARTUP_KIT_DIR` at
it. No credentials of its own; the mounted cert *is* the credential.

Submitting a job needs the built job definition too — mount `flare/jobs` and
point `FLARE_JOBS_DIR` at it (same convention as `flare/submit.sh`).

The federation must already be up (`docker compose up` in
`flare/workspace/mip/prod_00`) — `docker-compose.yaml` here joins its network
and mounts the admin startup kit and job definitions from it.

```sh
docker compose up --build -d

curl http://localhost:8080/health
curl http://localhost:8080/clients
curl http://localhost:8080/jobs
curl -X POST http://localhost:8080/jobs/counter
```

`FLARE_STARTUP_KIT_DIR` defaults to `/admin`, `FLARE_ADMIN_USER` to
`admin@flecke.xyz`, `FLARE_JOBS_DIR` to `/jobs` — override in
`docker-compose.yaml` if the project or admin username changes.
