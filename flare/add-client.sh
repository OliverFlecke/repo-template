#!/usr/bin/env bash
# Adds one new client to an already-running federation without disrupting the
# server or existing clients: re-provisions (certs are stable across re-runs,
# see provision.sh) into a new prod_NN, then grafts just the new client's kit
# and compose service into the live prod_00 and starts only that container.
set -euo pipefail
cd "$(dirname "$0")"

NAME="${1:?usage: add-client.sh <name> [org]}"
ORG="${2:-}"

LIVE_DIR=workspace/example_project/prod_00
if [ ! -d "$LIVE_DIR" ]; then
	echo "no live stack found at $LIVE_DIR - run provision.sh and docker compose up first" >&2
	exit 1
fi

# record the new client in project.yml so it's part of the roster for good
uv run python - "$NAME" "$ORG" <<'EOF'
import sys

import yaml

name, org = sys.argv[1], sys.argv[2]

with open("project.yml") as f:
	project = yaml.safe_load(f)

participants = project["participants"]
if any(p["name"] == name for p in participants):
	sys.exit(f"participant {name!r} already exists in project.yml")

if not org:
	org = next(p["org"] for p in participants if p["type"] == "client")

insert_at = next(i for i, p in enumerate(participants) if p["type"] == "admin")
participants.insert(insert_at, {"name": name, "type": "client", "org": org})


class IndentedDumper(yaml.SafeDumper):
	# PyYAML's default dumper doesn't indent list items under their parent
	# key, which rewrites project.yml's whole formatting on every save.
	def increase_indent(self, flow=False, indentless=False):
		return super().increase_indent(flow, False)


with open("project.yml", "w") as f:
	yaml.dump(project, f, Dumper=IndentedDumper, sort_keys=False, default_flow_style=False)
EOF

./provision.sh

NEW_DIR=$(ls -d workspace/example_project/prod_*/ | sort | tail -1)
NEW_DIR="${NEW_DIR%/}"
if [ "$NEW_DIR" = "$LIVE_DIR" ]; then
	echo "provisioning didn't produce a new prod_NN dir, something's wrong" >&2
	exit 1
fi

cp -r "$NEW_DIR/$NAME" "$LIVE_DIR/$NAME"

# graft just the new client's compose service into the live compose.yaml
uv run python - "$NEW_DIR" "$LIVE_DIR" "$NAME" <<'EOF'
import sys

import yaml

new_dir, live_dir, name = sys.argv[1], sys.argv[2], sys.argv[3]

with open(f"{new_dir}/compose.yaml") as f:
	new_compose = yaml.safe_load(f)

live_compose_path = f"{live_dir}/compose.yaml"
with open(live_compose_path) as f:
	live_compose = yaml.safe_load(f)

live_compose["services"][name] = new_compose["services"][name]

with open(live_compose_path, "w") as f:
	yaml.safe_dump(live_compose, f, sort_keys=False)
EOF

docker compose -f "$LIVE_DIR/compose.yaml" up -d "$NAME"

echo "$NAME is up - server and existing clients were not restarted"
