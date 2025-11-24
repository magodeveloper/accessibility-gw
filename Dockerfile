# ===== ETAPA BASE =====
# Usar la imagen base oficial de .NET 9
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Labels para metadata
LABEL maintainer="accessibility-team"
LABEL version="1.0"
LABEL description="Accessibility Gateway API"

WORKDIR /app
EXPOSE 8080

# Configurar timezone
ENV TZ=America/Mexico_City

# Instalar curl para health checks y configurar timezone
RUN apt-get update && \
    apt-get install -y curl tzdata && \
    ln -snf /usr/share/zoneinfo/"$TZ" /etc/localtime && \
    echo "$TZ" > /etc/timezone && \
    rm -rf /var/lib/apt/lists/*

# Variables de entorno
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_GENERATE_ASPNET_CERTIFICATE=false

# Crear usuario no-root para mayor seguridad
RUN adduser --disabled-password --home /app --gecos '' appuser && chown -R appuser /app
USER appuser

# ===== ETAPA BUILD =====
# Imagen para build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar archivos de gestión de paquetes centralizados
COPY ["Directory.Packages.props", "./"]

# Copiar proyecto del Gateway
COPY ["src/Gateway/Gateway.csproj", "src/Gateway/"]

# Restaurar dependencias
RUN dotnet restore "src/Gateway/Gateway.csproj"

# Copiar código fuente (excluye tests por .dockerignore)
COPY . .
WORKDIR "/src"

# Build de la aplicación
RUN dotnet build "src/Gateway/Gateway.csproj" -c "$BUILD_CONFIGURATION" -o /app/build --no-restore

# ===== ETAPA PUBLISH =====
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/Gateway/Gateway.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# ===== IMAGEN FINAL =====
FROM base AS final
WORKDIR /app

# Crear directorios necesarios
RUN mkdir -p /app/logs && chown -R appuser:appuser /app/logs

# Copiar archivos publicados
COPY --from=publish /app/publish .

# Health check optimizado
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Punto de entrada
ENTRYPOINT ["dotnet", "Gateway.dll"]