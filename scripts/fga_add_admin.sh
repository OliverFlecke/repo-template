#!/usr/bin/env sh

# Utility script to add a user to the system admin group

user_id=$1

FGA_API_URL="http://localhost:6080"
FGA_STORE_ID=$(fga store list --api-url $FGA_API_URL | jq -r ".stores[0].id")
FGA_MODEL_ID=$(fga model list --store-id=$FGA_STORE_ID --api-url $FGA_API_URL | jq ".authorization_models[0].id" -r)

fga tuple write \
	--store-id=$FGA_STORE_ID \
	--api-url $FGA_API_URL \
	--on-duplicate=ignore \
	"user:$user_id" admin system:core