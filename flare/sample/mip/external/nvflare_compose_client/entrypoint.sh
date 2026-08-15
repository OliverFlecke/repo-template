#!/bin/sh
set -eu
: "${NVFLARE_CLIENT_NAME:?NVFLARE_CLIENT_NAME must be set}"
: "${NVFLARE_ORG:?NVFLARE_ORG must be set}"
exec python3 -u -m nvflare.private.fed.app.client.client_train \
	-m /workspace -s fed_client.json --set secure_train=true \
	"uid=${NVFLARE_CLIENT_NAME}" "org=${NVFLARE_ORG}" config_folder=config
