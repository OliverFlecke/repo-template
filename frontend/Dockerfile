FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.24 AS api-builder
ENV DOTNET_RUNNING_IN_CONTAINER=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["*.props", "."]
COPY ["libs/OpenFGA/Api.OpenFGA.csproj", "libs/OpenFGA/"]
COPY ["api/Api/Api.csproj", "api/Api/"]
RUN dotnet restore "api/Api/Api.csproj"

# Copy everything else and build
COPY libs/ libs/
COPY api/ api/
WORKDIR "/src/"
RUN dotnet build "api/Api/Api.csproj" -c "${BUILD_CONFIGURATION}" -o /app/build

FROM ghcr.io/astral-sh/uv:python3.14-alpine AS flare-api-builder
WORKDIR /src
COPY flare-api/ .
RUN uv sync --frozen --no-cache && \
	uv run python export_openapi.py

# Build frontend
FROM node:26.5-alpine3.24 AS base
FROM base AS builder
ENV NEXT_TELEMETRY_DISABLED=1
ENV CI=true

WORKDIR /app

# TODO: ensure the version here is synced with mise.toml
RUN npm install -g pnpm@11
COPY frontend/package.json frontend/pnpm-*.yaml ./
RUN pnpm install --frozen-lockfile --yes

COPY --from=api-builder /src/api/Api/openapi.json ../api/Api/openapi.json
COPY --from=flare-api-builder /src/openapi.json ../flare-api/openapi.json
COPY frontend/ .

RUN OUTPUT_MODE=standalone pnpm build

# Production image, copy all the files and run next
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Set the correct permission for prerender cache
RUN mkdir .next && chown nextjs:nodejs .next

COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]