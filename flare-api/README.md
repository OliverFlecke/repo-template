# flare-api

FastAPI wrapper around the [FLARE](../flare) server's admin API.

Auth to the FLARE server is mTLS via a provisioned admin startup kit (see
`flare/submit.sh`) — mount it read-only and point `FLARE_STARTUP_KIT_DIR` at
it. No credentials of its own; the mounted cert *is* the credential.

```sh
docker build -t flare-api .
docker run --rm -p 8080:80 \
 --network prod_00_default \
 -v "$PWD/../flare/workspace/mip/prod_00/admin@flecke.xyz:/admin:ro" \
 -e FLARE_ADMIN_USER=admin@flecke.xyz \
 flare-api

curl http://localhost:8080/health
```

`FLARE_STARTUP_KIT_DIR` defaults to `/admin`, `FLARE_ADMIN_USER` to
`admin@flecke.xyz`.
