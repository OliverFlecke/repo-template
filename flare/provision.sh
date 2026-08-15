#!/usr/bin/env bash
# Provisions the local deployment, then writes our own compose.yaml plus two
# purpose-built Docker images (server, client) with the run command baked
# into each image's entrypoint. The client image is generic - it's
# parameterized at runtime by the NVFLARE_CLIENT_NAME/NVFLARE_ORG environment
# variables set per service in compose.yaml, so the same image runs every
# client site.
set -euo pipefail
cd "$(dirname "$0")"

uv run nvflare provision -p project.yml -w workspace --force

# each run lands in a new, incrementing prod_NN dir (prod_00, prod_01, ...) and
# never touches an earlier one, so build the images/compose file for whichever
# one this run just produced.
PROD_DIR=$(ls -d workspace/mip/prod_*/ | sort | tail -1)
PROD_DIR="${PROD_DIR%/}"
BASE_IMAGE=ghcr.io/astral-sh/uv:python3.14-bookworm-slim

uv run python - "$PROD_DIR" "$BASE_IMAGE" <<'EOF'
import sys
from pathlib import Path

import yaml

prod_dir, base_image = Path(sys.argv[1]), sys.argv[2]

project = yaml.safe_load(open("project.yml"))
participants = project["participants"]
server = next(p for p in participants if p["type"] == "server")
clients = [p for p in participants if p["type"] == "client"]

server_dir = prod_dir / "nvflare_compose_server"
client_dir = prod_dir / "nvflare_compose_client"
server_dir.mkdir(exist_ok=True)
client_dir.mkdir(exist_ok=True)

requirements = Path("requirements.txt").read_text()
(server_dir / "requirements.txt").write_text(requirements)
(client_dir / "requirements.txt").write_text(requirements)

(server_dir / "Dockerfile").write_text(f"""\
FROM {base_image}
COPY requirements.txt requirements.txt
RUN uv pip install --system -r requirements.txt
ENTRYPOINT ["python3", "-u", "-m", "nvflare.private.fed.app.server.server_train", \
"-m", "/workspace", "-s", "fed_server.json", "--set", "secure_train=true", \
"config_folder=config", "org={server["org"]}"]
""")

(client_dir / "entrypoint.sh").write_text("""\
#!/bin/sh
set -eu
: "${NVFLARE_CLIENT_NAME:?NVFLARE_CLIENT_NAME must be set}"
: "${NVFLARE_ORG:?NVFLARE_ORG must be set}"
exec python3 -u -m nvflare.private.fed.app.client.client_train \\
	-m /workspace -s fed_client.json --set secure_train=true \\
	"uid=${NVFLARE_CLIENT_NAME}" "org=${NVFLARE_ORG}" config_folder=config
""")
(client_dir / "entrypoint.sh").chmod(0o755)

(client_dir / "Dockerfile").write_text(f"""\
FROM {base_image}
COPY requirements.txt requirements.txt
RUN uv pip install --system -r requirements.txt
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
""")

fed_learn_port = server.get("fed_learn_port", 8002)
admin_port = server.get("admin_port", fed_learn_port)

services = {
	server["name"]: {
		"build": "nvflare_compose_server",
		"image": "nvflare-server",
		"container_name": server["name"],
		"ports": [f"{fed_learn_port}:{fed_learn_port}", f"{admin_port}:{admin_port}"],
		"volumes": [f"./{server['name']}:/workspace", "nvflare_svc_persist:/tmp/nvflare/"],
	}
}
for c in clients:
	services[c["name"]] = {
		"build": "nvflare_compose_client",
		"image": "nvflare-client",
		"container_name": c["name"],
		"environment": {"NVFLARE_CLIENT_NAME": c["name"], "NVFLARE_ORG": c["org"]},
		"volumes": [f"./{c['name']}:/workspace"],
	}

compose = {"services": services, "volumes": {"nvflare_svc_persist": None}}
yaml.safe_dump(compose, open(prod_dir / "compose.yaml", "w"), sort_keys=False)
EOF