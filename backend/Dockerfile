# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia e restaura dependências (camada de cache otimizada)
COPY TodoApi.csproj .
RUN dotnet restore

# Copia o restante e publica em Release
COPY . .
RUN dotnet publish TodoApi.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Cria diretório persistente para o banco de dados SQLite
RUN mkdir -p /app/data

COPY --from=build /app/publish .

# Variáveis padrão (sobrescritas pelo docker-compose)
ENV APP_PORT=5000 \
    DB_PATH=/app/data/todos.db \
    LOG_LEVEL=Information

EXPOSE 5000

ENTRYPOINT ["dotnet", "TodoApi.dll"]
