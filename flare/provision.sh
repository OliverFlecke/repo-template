#!/usr/bin/env bash
# Provisions the local deployment, then swaps NVFlare's generated pip-based
# Dockerfile for a uv-based one (NVFlare's DockerBuilder always writes a
# fixed pip Dockerfile; there's no project.yml knob for that, so we overwrite
# it after the fact).
set -euo pipefail
cd "$(dirname "$0")"

uv run nvflare provision -p project.yml -w workspace --force

# each run lands in a new, incrementing prod_NN dir (prod_00, prod_01, ...) and never
# touches an earlier one, so patch whichever one this run just produced.
PROD_DIR=$(ls -d workspace/example_project/prod_*/ | sort | tail -1)
DOCKERFILE="${PROD_DIR}nvflare_compose/Dockerfile"
cat >"$DOCKERFILE" <<'EOF'
FROM ghcr.io/astral-sh/uv:python3.14-bookworm-slim
COPY requirements.txt requirements.txt
RUN uv pip install --system -r requirements.txt
EOF
