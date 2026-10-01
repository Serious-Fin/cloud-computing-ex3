# Build stage. The build context is the repository root (see render.yaml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the project file alone so the layer is cached until packages change.
COPY api/api.csproj api/
RUN dotnet restore api/api.csproj

COPY api/ api/
RUN dotnet publish api/api.csproj -c Release -o /app/publish --no-restore

# Runtime stage.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

# Render passes the port to listen on in PORT; the base image sets ASPNETCORE_HTTP_PORTS
# to 8080, so unset it to make the port below the only one Kestrel binds.
CMD ["/bin/sh", "-c", "unset ASPNETCORE_HTTP_PORTS; exec dotnet api.dll --urls http://0.0.0.0:${PORT:-10000}"]
