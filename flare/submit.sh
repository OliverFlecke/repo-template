#!/usr/bin/env bash
# Builds the counter job and submits it to the running docker-compose federation.
set -euo pipefail
cd "$(dirname "$0")"

PROD_DIR=workspace/example_project/prod_00
if [ ! -d "$PROD_DIR" ]; then
	echo "no provisioned workspace found, run: uv run nvflare provision -p project.yml -w workspace" >&2
	exit 1
fi

uv run python -m flare.counter

docker run --rm \
	--network prod_00_default \
	-v "$PWD/$PROD_DIR/admin@flecke.xyz:/admin" \
	-v "$PWD/jobs:/jobs" \
	--entrypoint nvflare \
	nvflare-client \
	job submit -j /jobs/counter --startup-kit /admin