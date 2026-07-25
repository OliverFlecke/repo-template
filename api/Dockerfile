FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ENV DOTNET_RUNNING_IN_CONTAINER=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["*.props", "."]
COPY ["api/Api/Api.csproj", "Api/"]
RUN dotnet restore "Api/Api.csproj"

# Copy everything else and build
COPY api/ .
WORKDIR "/src/Api"
RUN dotnet build "Api.csproj" -c "${BUILD_CONFIGURATION}" -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" -c "${BUILD_CONFIGURATION}" -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && \
    chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Api.dll"]