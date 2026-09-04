@echo off
setlocal enabledelayedexpansion

:: Posizionati nella directory root del repository
cd /d "%~dp0"

echo ==================================================
echo    CardMaker -- Avvio Host Desktop (Photino)
echo ==================================================

:: 1. Verifica presenza di .NET SDK
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [ERRORE] .NET SDK non trovato nel PATH.
    echo Installa .NET 10 SDK da https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set DOTNET_VER=%%v
echo [.NET SDK rilevato: %DOTNET_VER%]

:: 2. Pulizia build precedenti
echo.
echo [1/3] Pulizia dei binari di compilazione...
dotnet clean CardMaker.slnx -v q --nologo

:: 3. Ripristino pacchetti NuGet
echo [2/3] Ripristino dei pacchetti NuGet...
dotnet restore CardMaker.slnx --nologo
if %ERRORLEVEL% neq 0 (
    echo [ERRORE] Ripristino pacchetti fallito.
    pause
    exit /b %ERRORLEVEL%
)

:: 4. Avvio dell'applicazione Desktop
echo [3/3] Compilazione e avvio di CardMaker...
echo Apertura finestra desktop nativa in-process (Photino.Blazor)...
echo --------------------------------------------------

dotnet run --project src\CardMaker.Desktop\CardMaker.Desktop.csproj

if %ERRORLEVEL% neq 0 (
    echo.
    echo [CardMaker terminato con codice %ERRORLEVEL%]
    pause
)
