# syntax=docker/dockerfile:1

# ---- build: restaura y publica en Release ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo el csproj para cachear el restore.
COPY backend-asa.csproj ./
RUN dotnet restore backend-asa.csproj

COPY . .
RUN dotnet publish backend-asa.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime: solo ASP.NET Core, usuario no root ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# curl solo para el HEALTHCHECK.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# APP_UID lo define la imagen base de .NET (usuario "app", sin privilegios).
USER $APP_UID

HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "backend-asa.dll"]
