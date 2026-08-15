# flare-api

FastAPI wrapper around the [FLARE](../flare) server's admin API.

Auth to the FLARE server is mTLS via a provisioned admin startup kit (see
`flare/submit.sh`) — mount it read-only and point `FLARE_STARTUP_KIT_DIR` at
it. No credentials of its own; the mounted cert *is* the credential.

Submitting a job needs the built job definition too — mount `flare/jobs` and
point `FLARE_JOBS_DIR` at it (same convention as `flare/submit.sh`).

Provisioning a client needs read-write access to `flare/project.yml` and
`flare/workspace` too — mount them at `FLARE_PROJECT_YML` (default
`/flare-project/project.yml`) and `FLARE_WORKSPACE_DIR` (default
`/flare-project/workspace`). It shells out to the `nvflare provision` CLI
(from flare-api's own `nvflare` dependency), which is why it needs the same
`project.yml` and `workspace` the federation itself was provisioned from -
re-provisioning reuses the existing root CA, so the new client's cert comes
out trusted by the already-running server without restarting it (see
`flare/README.md`).

The federation must already be up (`docker compose up` in
`flare/workspace/mip/prod_00`) — `docker-compose.yaml` here joins its network
and mounts the admin startup kit and job definitions from it.

```sh
docker compose up --build -d

curl http://localhost:8090/health
curl http://localhost:8090/clients
curl http://localhost:8090/jobs
curl -X POST http://localhost:8090/jobs/counter
curl -X POST http://localhost:8090/clients/site-6 -o site-6.zip
```

`FLARE_STARTUP_KIT_DIR` defaults to `/admin`, `FLARE_ADMIN_USER` to
`admin@flecke.xyz`, `FLARE_JOBS_DIR` to `/jobs` — override in
`docker-compose.yaml` if the project or admin username changes.

## OpenAPI client

`export_openapi.py` writes `openapi.json` (gitignored, like the .NET api's),
which the frontend's `openapi-ts` (`pnpm build:api`) reads to generate the
TypeScript client - regenerate it first whenever an endpoint changes:

```sh
uv run python export_openapi.py
```
