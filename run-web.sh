#!/usr/bin/env bash
set -euo pipefail

# Spostati nella cartella root del progetto
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=================================================="
echo "   🃏 CardMaker — Avvio Host Web (ASP.NET Core)   "
echo "=================================================="

# 1. Verifica presenza di .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "❌ Errore: .NET SDK non trovato nel PATH."
    echo "Installa .NET 10 SDK da https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VER=$(dotnet --version)
echo "ℹ️  .NET SDK rilevato: $DOTNET_VER"

# 2. Pulizia build precedenti
echo ""
echo "🧹 [1/3] Pulizia dei binari di compilazione..."
dotnet clean CardMaker.slnx -v q --nologo

# 3. Ripristino pacchetti NuGet
echo "📦 [2/3] Ripristino dei pacchetti NuGet..."
dotnet restore CardMaker.slnx --nologo

# 4. Avvio dell'applicazione Web
echo "🚀 [3/3] Compilazione e avvio di CardMaker.Web..."
echo "L'applicazione è disponibile su: http://localhost:5240"
echo "Premi CTRL+C per arrestare il server."
echo "--------------------------------------------------"

ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/CardMaker.Web/CardMaker.Web.csproj --no-launch-profile --urls "http://localhost:5240"
