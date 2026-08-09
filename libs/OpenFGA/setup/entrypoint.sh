#!/bin/sh
set -eu

API_URL="${FGA_API_URL:-http://openfga:8080}"
STORE_NAME="${FGA_STORE_NAME:-repo-template}"
MODEL_FILE="${FGA_MODEL_FILE:-/model/fga.mod}"
OUT_FILE="${FGA_OUTPUT_FILE:-/shared/openfga.env}"

echo "Waiting for OpenFGA at ${API_URL}..."
until curl -sf "${API_URL}/healthz" >/dev/null 2>&1; do
	sleep 1
done

# Look for an already-provisioned store so repeated `docker compose up` runs
# (without a full teardown) reuse it instead of piling up duplicates.
STORE_ID=$(
	curl -sf "${API_URL}/stores" \
		| jq -r --arg name "${STORE_NAME}" '.stores[]? | select(.name == $name) | .id' \
		| head -n1
)

if [ -z "${STORE_ID}" ]; then
	echo "Creating OpenFGA store '${STORE_NAME}' from ${MODEL_FILE}..."
	RESULT=$(fga store create --api-url "${API_URL}" --name "${STORE_NAME}" --model "${MODEL_FILE}")
	STORE_ID=$(echo "${RESULT}" | jq -r '.store.id')
	MODEL_ID=$(echo "${RESULT}" | jq -r '.model.authorization_model_id')
else
	echo "Reusing OpenFGA store '${STORE_NAME}' (${STORE_ID}), writing latest model from ${MODEL_FILE}..."
	MODEL_ID=$(
		fga model write --api-url "${API_URL}" --store-id "${STORE_ID}" --file "${MODEL_FILE}" \
			| jq -r '.authorization_model_id'
	)
fi

mkdir -p "$(dirname "${OUT_FILE}")"
cat > "${OUT_FILE}" <<EOF
OpenFga__StoreId=${STORE_ID}
OpenFga__ModelId=${MODEL_ID}
EOF

echo "OpenFGA ready: store_id=${STORE_ID} model_id=${MODEL_ID}"
