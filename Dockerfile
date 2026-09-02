# Multi-stage Dockerfile per CardMaker.Web (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia solution e file di progetto per caching layer NuGet
COPY ["CardMaker.slnx", "./"]
COPY ["src/CardMaker.Domain/CardMaker.Domain.csproj", "src/CardMaker.Domain/"]
COPY ["src/CardMaker.Contracts/CardMaker.Contracts.csproj", "src/CardMaker.Contracts/"]
COPY ["src/CardMaker.Rendering/CardMaker.Rendering.csproj", "src/CardMaker.Rendering/"]
COPY ["src/CardMaker.Application/CardMaker.Application.csproj", "src/CardMaker.Application/"]
COPY ["src/CardMaker.Infrastructure/CardMaker.Infrastructure.csproj", "src/CardMaker.Infrastructure/"]
COPY ["src/CardMaker.UI/CardMaker.UI.csproj", "src/CardMaker.UI/"]
COPY ["src/CardMaker.Web/CardMaker.Web.csproj", "src/CardMaker.Web/"]
COPY ["src/CardMaker.Desktop/CardMaker.Desktop.csproj", "src/CardMaker.Desktop/"]

RUN dotnet restore "src/CardMaker.Web/CardMaker.Web.csproj"

# Copia sorgenti completi e pubblica
COPY . .
WORKDIR "/src/src/CardMaker.Web"
RUN dotnet publish "CardMaker.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage finale runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Installa librerie C native indispensabili per il rendering SkiaSharp e font su Linux
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       libfontconfig1 \
       libfreetype6 \
       fonts-dejavu-core \
       curl \
    && rm -rf /var/lib/apt/lists/*

# Crea cartella dati persistente e utente non-root
RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    Storage__DataRoot=/app/data

VOLUME ["/app/data"]
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "CardMaker.Web.dll"]

