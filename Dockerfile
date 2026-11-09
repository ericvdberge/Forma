# Forma in one image: the CMS app (nginx), the API (.NET) and media storage (versitygw, S3-compatible),
# kept running by supervisord. Build from the repository root:
#   docker build -t forma .
#   docker run -p 8080:8080 -v forma-assets:/app/assets forma
# Uploaded images and thumbnails are plain files under /app/assets, a volume.

# --- CMS app (Vue) ---------------------------------------------------------------------------
FROM node:22-bookworm-slim AS app-build
WORKDIR /src

# Every workspace's package.json is needed for `npm ci`; sources only for what the app uses.
COPY package.json package-lock.json ./
COPY src/design-system/package.json src/design-system/
COPY src/landing/package.json src/landing/
COPY src/app/package.json src/app/
RUN npm ci

COPY src/design-system src/design-system
COPY src/app src/app
RUN npm run build:app

# --- API (.NET) ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY src/api/Forma.Api.slnx ./
COPY src/api/Forma.Api/Forma.Api.csproj Forma.Api/
COPY src/api/Forma.Application/Forma.Application.csproj Forma.Application/
COPY src/api/Forma.Infrastructure/Forma.Infrastructure.csproj Forma.Infrastructure/
RUN dotnet restore Forma.Api/Forma.Api.csproj

COPY src/api .
# The OpenAPI document is generated during development builds and committed; skip it here.
RUN dotnet publish Forma.Api/Forma.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/api \
    -p:OpenApiGenerateDocuments=false

# --- Runtime ---------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx supervisor \
    && rm -rf /var/lib/apt/lists/*

# S3-compatible gateway that stores each object as a file: bucket "media" is /app/assets/media.
COPY --from=versity/versitygw:v1.10.0 /usr/local/bin/versitygw /usr/local/bin/versitygw

COPY docker/nginx.conf /etc/nginx/nginx.conf
COPY docker/supervisord.conf /etc/supervisor/forma.conf
COPY --chmod=755 docker/entrypoint.sh /usr/local/bin/forma-entrypoint
COPY --from=app-build /src/src/app/dist /app/wwwroot
COPY --from=api-build /app/api /app/api

# Owned by the non-root `app` user, so a fresh named volume inherits that ownership.
RUN mkdir -p /app/assets && chown app:app /app/assets
VOLUME /app/assets

USER app
EXPOSE 8080
ENTRYPOINT ["forma-entrypoint"]
