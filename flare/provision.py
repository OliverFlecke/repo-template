#!/usr/bin/env -S uv run
# Provisions the local deployment, then writes our own compose.yaml and
# populates each prod_NN's nvflare_compose_server/nvflare_compose_client dirs
# from server_image/ and client_image/ - static Dockerfiles/entrypoints,
# parameterized at runtime by the NVFLARE_ORG/NVFLARE_CLIENT_NAME env vars
# compose.yaml sets per service, so the same two images run every site.

import os
import subprocess
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parent
os.chdir(ROOT)

subprocess.run(
    ["nvflare", "provision", "-p", "project.yml", "-w", "workspace", "--force"],
    check=True,
)

# each run lands in a new, incrementing prod_NN dir (prod_00, prod_01, ...) and
# never touches an earlier one, so populate whichever one this run just produced.
prod_dir = max(Path("workspace/mip").glob("prod_*"))

with open("project.yml") as f:
    project = yaml.safe_load(f)

participants = project["participants"]
server = next(p for p in participants if p["type"] == "server")
clients = [p for p in participants if p["type"] == "client"]

server_dir = prod_dir / "nvflare_compose_server"
client_dir = prod_dir / "nvflare_compose_client"
server_dir.mkdir(exist_ok=True)
client_dir.mkdir(exist_ok=True)

requirements = Path("requirements.txt").read_text()
(server_dir / "requirements.txt").write_text(requirements)
(client_dir / "requirements.txt").write_text(
    requirements + "fastapi[standard]==0.141.1\nsupervisor==4.2.5\nsetuptools<81\n"
)

for name in ("Dockerfile", "entrypoint.sh"):
    dest = server_dir / name
    dest.write_bytes((ROOT / "server_image" / name).read_bytes())
    dest.chmod((ROOT / "server_image" / name).stat().st_mode)

for name in ("Dockerfile", "entrypoint.sh", "client_api.py", "supervisord.conf"):
    dest = client_dir / name
    dest.write_bytes((ROOT / "client_image" / name).read_bytes())
    dest.chmod((ROOT / "client_image" / name).stat().st_mode)

fed_learn_port = server.get("fed_learn_port", 8002)
admin_port = server.get("admin_port", fed_learn_port)

services = {
    server["name"]: {
        "build": "nvflare_compose_server",
        "image": "nvflare-server",
        "container_name": server["name"],
        "environment": {"NVFLARE_ORG": server["org"]},
        "ports": [f"{fed_learn_port}:{fed_learn_port}", f"{admin_port}:{admin_port}"],
        "volumes": [
            f"./{server['name']}:/workspace",
            "nvflare_svc_persist:/tmp/nvflare/",
        ],
    }
}
volumes = {"nvflare_svc_persist": None}
for i, c in enumerate(clients):
    data_volume = f"{c['name']}_data"
    volumes[data_volume] = None
    services[c["name"]] = {
        "build": "nvflare_compose_client",
        "image": "nvflare-client",
        "container_name": c["name"],
        "environment": {"NVFLARE_CLIENT_NAME": c["name"], "NVFLARE_ORG": c["org"]},
        "volumes": [f"./{c['name']}:/workspace", f"{data_volume}:/data"],
        "ports": [f"127.0.0.1:{8091 + i}:8080"],
    }

compose = {"name": "flare", "services": services, "volumes": volumes}
with open(prod_dir / "compose.yaml", "w") as f:
    yaml.safe_dump(compose, f, sort_keys=False)