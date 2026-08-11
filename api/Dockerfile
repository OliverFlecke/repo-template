FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.24 AS build
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
# Pre-generate Wolverine's handler/routing code (runs its own implicit build to the
# default bin/ output first), then do the real build. Release builds don't reference
# WolverineFx.RuntimeCompilation (see Api.csproj), so without this, message routing for
# locally-published messages (e.g. IMartenOutbox.PublishAsync) silently resolves to
# "No Routes" and those messages are dropped instead of reaching their handlers. Codegen
# must run before the real build so the generated files are on disk to be picked up by it.
WORKDIR "/src/api/Api"
RUN dotnet run -- codegen write && \
	dotnet build -c "${BUILD_CONFIGURATION}" -o /app/build

FROM build AS publish
RUN dotnet publish -c "${BUILD_CONFIGURATION}" -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.24 AS final
WORKDIR /app

RUN apk add curl=8.21.0-r0 --no-cache && \
	adduser --disabled-password --gecos "" appuser && \
	chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["./Api"]