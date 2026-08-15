# flare

Minimal NVIDIA FLARE setup: a server and a few clients as separate Docker
containers talking real (TLS) gRPC, running a trivial "counter" job — each
round every client sends back `1`, the server sums them.

## Job

`src/flare/counter.py` has `CounterController` (server) and `CounterExecutor`
(client), plus a `main()` that builds the job and exports it to `jobs/`.

## Run it

```sh
uv sync

# 1. provision a local "production-like" deployment: 1 server + 3 clients,
#    with real TLS certs, into workspace/example_project/prod_00
#    (containers run Python 3.14 via uv, see provision.py)
./provision.py

# builds two images: nvflare-server and nvflare-client. The client image is
# generic - client_train's command (including `uid=...`) is baked into its
# entrypoint, parameterized by the NVFLARE_CLIENT_NAME/NVFLARE_ORG env vars
# compose.yaml sets per service - so the same image runs every client site.

# 2. build and start the server + client containers
cd workspace/example_project/prod_00
docker compose up --build -d
cd -

# 3. build the job and submit it to the running federation
./submit.sh

# watch it run
docker compose -f workspace/example_project/prod_00/compose.yaml logs -f server

# tear down
docker compose -f workspace/example_project/prod_00/compose.yaml down
```

`workspace/` and `jobs/` are generated (gitignored) — re-run the commands
above to regenerate them. To change the number of clients before first
provisioning, edit the `participants` list in `project.yml` and re-provision.

## Adding a client to a running federation

`workspace/example_project/prod_00` is the **live** compose stack — the one
`docker compose up` above started. Every subsequent `nvflare provision` run
(certs are cheap and stable: re-provisioning reuses the existing root CA and
reissues byte-identical certs for unchanged participants) lands in a new
`prod_01`, `prod_02`, ... and never touches `prod_00`.

`add-client.sh` uses that to onboard one new site without touching the
server or any already-connected client:

```sh
./add-client.sh site-4
```

It adds `site-4` to `project.yml`, re-provisions into a fresh `prod_NN`,
copies just `site-4`'s signed startup kit and compose service block into the
live `prod_00`, and starts only that container. `server`/`site-1..3` are
never restarted — the server has no static client roster, it just accepts
any connection presenting a cert signed by the trusted root CA.
