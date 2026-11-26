# syntax=docker/dockerfile:1.7

# -----------------------------------------------------------------------------
# Stage 1: Build Angular PWA (frontend/)
# -----------------------------------------------------------------------------
FROM node:22-bookworm-slim AS frontend-build

WORKDIR /src/frontend

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY frontend/ ./
RUN npm run build

# -----------------------------------------------------------------------------
# Stage 2: Publish .NET 10 WebApi (backend/src/H2l.WebApi) + bundle PWA assets
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS backend-build

WORKDIR /src/backend

COPY backend/Directory.Build.props ./
COPY backend/src/H2l.Domain/H2l.Domain.csproj src/H2l.Domain/
COPY backend/src/H2l.Application/H2l.Application.csproj src/H2l.Application/
COPY backend/src/H2l.Infrastructure/H2l.Infrastructure.csproj src/H2l.Infrastructure/
COPY backend/src/H2l.WebApi/H2l.WebApi.csproj src/H2l.WebApi/

RUN dotnet restore src/H2l.WebApi/H2l.WebApi.csproj

COPY backend/src/ src/
RUN dotnet publish src/H2l.WebApi/H2l.WebApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

COPY --from=frontend-build /src/frontend/dist/h2l-web/browser /app/publish/wwwroot

# -----------------------------------------------------------------------------
# Stage 3: All-in-One Non-Root Runtime (.NET 10 + optional embedded Postgres 17)
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime

LABEL org.opencontainers.image.title="h2l" \
      org.opencontainers.image.description="h2l — Human-in-the-Loop Workflow Integrator (All-in-One)" \
      org.opencontainers.image.source="https://github.com/dariuszkusz/h2l"

ARG DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        gnupg \
    && install -d -m 0755 /usr/share/postgresql-common/pgdg \
    && curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc \
        -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
    && echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt noble-pgdg main" \
        > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
        postgresql-17 \
        postgresql-client-17 \
    && pg_dropcluster --stop 17 main || true \
    && apt-get purge -y --auto-remove gnupg \
    && rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

RUN groupadd --gid 10001 h2l \
    && useradd --uid 10001 --gid 10001 --home-dir /data --shell /usr/sbin/nologin --create-home h2l \
    && install -d -o h2l -g h2l -m 0750 /app /data /data/postgres /data/attachments \
    && install -d -o h2l -g h2l -m 0775 /var/run/postgresql

WORKDIR /app

COPY --from=backend-build --chown=h2l:h2l /app/publish ./
COPY --chown=h2l:h2l docker/entrypoint.sh /usr/local/bin/h2l-entrypoint.sh
RUN chmod 0755 /usr/local/bin/h2l-entrypoint.sh

ENV ASPNETCORE_URLS="http://+:8080" \
    ASPNETCORE_ENVIRONMENT="Production" \
    ATTACHMENTS_STORAGE_PATH="/data/attachments" \
    PGDATA="/data/postgres" \
    PATH="/usr/lib/postgresql/17/bin:${PATH}"

EXPOSE 8080

VOLUME ["/data"]

USER h2l

HEALTHCHECK --interval=30s --timeout=5s --start-period=45s --retries=3 \
    CMD curl -fsS http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["/usr/local/bin/h2l-entrypoint.sh"]
