#!/bin/sh
set -eu
: "${NVFLARE_ORG:?NVFLARE_ORG must be set}"
exec python3 -u -m nvflare.private.fed.app.server.server_train \
	-m /workspace -s fed_server.json --set secure_train=true \
	config_folder=config "org=${NVFLARE_ORG}"
