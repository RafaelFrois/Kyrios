#!/usr/bin/env bash
# Gera a versão web (navegador / Poki) do MegRace.
# Resultado: dist/web (pasta pronta pra testar) e dist/MegRace-web.zip (index.html na raiz — o formato que o
# Poki Inspector e o envio pra Poki esperam).
set -euo pipefail
root="$(cd "$(dirname "$0")" && pwd)"
rm -rf "$root/dist/web-publish" "$root/dist/web" "$root/dist/MegRace-web.zip"
dotnet publish "$root/src/Kyrios.Game.Web" -c Release -o "$root/dist/web-publish"
mv "$root/dist/web-publish/wwwroot" "$root/dist/web"
rm -rf "$root/dist/web-publish"
(cd "$root/dist/web" && zip -qr9 "$root/dist/MegRace-web.zip" .)
echo "Pronto: dist/web ($(du -sh "$root/dist/web" | cut -f1)) e dist/MegRace-web.zip"
echo "Teste local: (cd dist/web && python3 -m http.server 8080) -> http://localhost:8080"
