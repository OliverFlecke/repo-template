#!/usr/bin/env bash
# Provisions the local deployment, then swaps NVFlare's generated pip-based
# Dockerfile for a uv-based one (NVFlare's DockerBuilder always writes a
# fixed pip Dockerfile; there's no project.yml knob for that, so we overwrite
# it after the fact).
set -euo pipefail
cd "$(dirname "$0")"

uv run nvflare provision -p project.yml -w workspace --force

DOCKERFILE=workspace/example_project/prod_00/nvflare_compose/Dockerfile
cat >"$DOCKERFILE" <<'EOF'
FROM ghcr.io/astral-sh/uv:python3.14-bookworm-slim
COPY requirements.txt requirements.txt
RUN uv pip install --system -r requirements.txt
EOF
