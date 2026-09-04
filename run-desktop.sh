#!/usr/bin/env bash
set -euo pipefail

# Spostati nella cartella root del progetto
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=================================================="
echo "   💻 CardMaker — Avvio Host Desktop (Photino)    "
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

# 4. Registrazione ambiente desktop Linux (icone e launcher FreeDesktop)
if [[ "$(uname -s)" == "Linux" ]]; then
    DESKTOP_DIR="${HOME}/.local/share/applications"
    ICONS_DIR="${HOME}/.local/share/icons/hicolor"
    mkdir -p "$DESKTOP_DIR" "$ICONS_DIR"
    cp -f src/CardMaker.Desktop/Resources/cardmaker.desktop "${DESKTOP_DIR}/cardmaker.desktop" 2>/dev/null || true
    cp -f src/CardMaker.Desktop/Resources/cardmaker.desktop "${DESKTOP_DIR}/CardMaker.desktop" 2>/dev/null || true
    if [[ -d "src/CardMaker.Desktop/Resources/icons/hicolor" ]]; then
        for sz in 16x16 32x32 48x48 64x64 128x128 256x256 512x512; do
            if [[ -f "src/CardMaker.Desktop/Resources/icons/hicolor/${sz}/apps/cardmaker.png" ]]; then
                mkdir -p "${ICONS_DIR}/${sz}/apps"
                cp -f "src/CardMaker.Desktop/Resources/icons/hicolor/${sz}/apps/cardmaker.png" "${ICONS_DIR}/${sz}/apps/cardmaker.png" 2>/dev/null || true
                cp -f "src/CardMaker.Desktop/Resources/icons/hicolor/${sz}/apps/cardmaker.png" "${ICONS_DIR}/${sz}/apps/CardMaker.png" 2>/dev/null || true
            fi
        done
    fi
    command -v update-desktop-database &>/dev/null && update-desktop-database "$DESKTOP_DIR" 2>/dev/null || true
    command -v gtk-update-icon-cache &>/dev/null && gtk-update-icon-cache -q -t -f "$ICONS_DIR" 2>/dev/null || true
fi

# 5. Avvio dell'applicazione Desktop
echo "🚀 [3/3] Compilazione e avvio di CardMaker..."
echo "Apertura finestra desktop nativa in-process (Photino.Blazor)..."
echo "--------------------------------------------------"

dotnet run --project src/CardMaker.Desktop/CardMaker.Desktop.csproj
