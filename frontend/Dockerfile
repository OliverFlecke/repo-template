FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.24 AS api-builder
ENV DOTNET_RUNNING_IN_CONTAINER=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["*.props", "."]
COPY ["api/Api/Api.csproj", "Api/"]
RUN dotnet restore "Api/Api.csproj"

COPY api/ .
WORKDIR "/src/Api"
RUN dotnet build "Api.csproj" -c "${BUILD_CONFIGURATION}" -o /app/build

FROM node:26.5-alpine3.24 AS base

FROM base AS builder
ENV NEXT_TELEMETRY_DISABLED=1
ENV CI=true

WORKDIR /app

# TODO: ensure the version here is synced with mise.toml
RUN npm install -g pnpm@11
COPY frontend/package.json frontend/pnpm-*.yaml ./
RUN pnpm install --frozen-lockfile --yes

COPY --from=api-builder /src/Api/openapi.json ../api/Api/openapi.json
COPY frontend/ .

RUN OUTPUT_MODE=standalone pnpm build

# Production image, copy all the files and run next
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public

# Set the correct permission for prerender cache
RUN mkdir .next && chown nextjs:nodejs .next

COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]