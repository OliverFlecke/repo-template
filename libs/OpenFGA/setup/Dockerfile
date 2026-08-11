# The fga CLI image ships only the static binary (no shell), so it can't run
# our bootstrap script directly. Copy it onto a shell-capable base instead.
FROM openfga/cli:v0.7.19 AS fga

FROM alpine:3.21
RUN apk add --no-cache curl=8.14.1-r2 jq=1.7.1-r0
COPY --from=fga /fga /usr/local/bin/fga
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
