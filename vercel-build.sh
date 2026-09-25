#!/usr/bin/env bash
# Build da versão web na Vercel (ou em qualquer servidor Linux sem .NET instalado).
# Instala o .NET 8 SDK se ele não existir, publica o projeto web e deixa a pasta final em dist/web.
set -euo pipefail
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1   # a imagem de build da Vercel não tem ICU

if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks | grep -q '^8\.'; then
    echo "Instalando o .NET 8 SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$HOME/.dotnet:$PATH"
fi

dotnet --version
rm -rf dist/web dist/web-publish
dotnet publish src/Kyrios.Game.Web -c Release -o dist/web-publish
mv dist/web-publish/wwwroot dist/web
rm -rf dist/web-publish
echo "Versão web pronta em dist/web ($(du -sh dist/web | cut -f1))"
