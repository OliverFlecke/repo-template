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
#    (containers run Python 3.14 via uv, see provision.sh)
./provision.sh

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
above to regenerate them. To change the number of clients, edit the
`participants` list in `project.yml` and re-provision.
