FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.24 AS build
ENV DOTNET_RUNNING_IN_CONTAINER=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ARG BUILD_CONFIGURATION=Release

RUN apk update && \
	apk upgrade && \
	apk add clang=22.1.3-r2 build-base=0.5-r4 zlib-dev=1.3.2-r0 --no-cache && \
	rm -r /var/cache/apk/*

WORKDIR /src

COPY ["*.props", "/"]
COPY ["api/Api/Api.csproj", "Api/"]
COPY ["libs/OpenFGA/Api.OpenFGA.csproj", "/libs/OpenFGA/"]
RUN dotnet restore "Api/Api.csproj"

# Copy everything else and build
COPY api/ .
COPY libs/ /libs/
WORKDIR "/src/Api"

# Pre-generate Wolverine's handler/routing code (runs its own implicit build to the
# default bin/ output first), then do the real build. Release builds don't reference
# WolverineFx.RuntimeCompilation (see Api.csproj), so without this, message routing for
# locally-published messages (e.g. IMartenOutbox.PublishAsync) silently resolves to
# "No Routes" and those messages are dropped instead of reaching their handlers. Codegen
# must run before the real build so the generated files are on disk to be picked up by it.
RUN dotnet run -c "${BUILD_CONFIGURATION}" -- codegen write && \
	dotnet build "Api.csproj" -c "${BUILD_CONFIGURATION}" -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" -c "${BUILD_CONFIGURATION}" -o /app/publish /p:PublishAOT=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.24 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && \
    chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Api.dll"]