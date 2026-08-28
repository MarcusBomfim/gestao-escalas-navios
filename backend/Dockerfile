FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace

COPY global.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props backend/
COPY backend/src/PortManagement.Domain/PortManagement.Domain.csproj backend/src/PortManagement.Domain/
COPY backend/src/PortManagement.Application/PortManagement.Application.csproj backend/src/PortManagement.Application/
COPY backend/src/PortManagement.Infrastructure/PortManagement.Infrastructure.csproj backend/src/PortManagement.Infrastructure/
COPY backend/src/PortManagement.Api/PortManagement.Api.csproj backend/src/PortManagement.Api/

RUN dotnet restore backend/src/PortManagement.Api/PortManagement.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/src/PortManagement.Api/PortManagement.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
LABEL org.opencontainers.image.title="Port Management API" \
      org.opencontainers.image.description="API do Sistema de Gestão de Escalas de Navios" \
      org.opencontainers.image.source="https://github.com/MarcusBomfim/gestao-escalas-navios"
COPY --from=build /app/publish .
RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgssapi-krb5-2 wget \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data-protection-keys \
    && chown "$APP_UID:$APP_UID" /app/data-protection-keys
USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PortManagement.Api.dll"]
