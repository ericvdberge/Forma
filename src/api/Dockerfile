# Forma API image. Build from src/api:
#   docker build -t forma-api src/api
# Uploaded media is written to /app/assets, a volume:
#   docker run -p 8080:8080 -v forma-assets:/app/assets forma-api

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY Forma.Api.slnx ./
COPY Forma.Api/Forma.Api.csproj Forma.Api/
COPY Forma.Application/Forma.Application.csproj Forma.Application/
COPY Forma.Infrastructure/Forma.Infrastructure.csproj Forma.Infrastructure/
RUN dotnet restore Forma.Api/Forma.Api.csproj

COPY . .
# The OpenAPI document is generated during development builds and committed; skip it here.
RUN dotnet publish Forma.Api/Forma.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:OpenApiGenerateDocuments=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    Assets__Path=/app/assets

# Create the assets folder owned by the non-root `app` user, so a fresh named volume
# inherits that ownership and the API can write uploads to it.
RUN mkdir -p /app/assets && chown app:app /app/assets
VOLUME /app/assets

COPY --from=build /app/publish .

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Forma.Api.dll"]
