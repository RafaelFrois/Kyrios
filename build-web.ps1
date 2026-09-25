# Gera a versão web (navegador / Poki) do MegRace.
# Resultado: dist/web (pasta pronta pra testar) e dist/MegRace-web.zip (index.html na raiz — o formato que o
# Poki Inspector e o envio pra Poki esperam).
# Uso: no PowerShell, na pasta do repositório:  ./build-web.ps1
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out = Join-Path $root 'dist/web-publish'
$web = Join-Path $root 'dist/web'
$zip = Join-Path $root 'dist/MegRace-web.zip'

Remove-Item -Recurse -Force $out, $web -ErrorAction SilentlyContinue
Remove-Item -Force $zip -ErrorAction SilentlyContinue

dotnet publish (Join-Path $root 'src/Kyrios.Game.Web') -c Release -o $out
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish falhou' }

Copy-Item -Recurse (Join-Path $out 'wwwroot') $web
Remove-Item -Recurse -Force $out
Compress-Archive -Path (Join-Path $web '*') -DestinationPath $zip

$size = (Get-ChildItem -Recurse $web | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host ("Pronto: {0} ({1:N2} MB) e {2}" -f $web, $size, $zip)
Write-Host 'Teste local: dotnet tool install -g dotnet-serve ; dotnet serve -d dist/web -p 8080  -> http://localhost:8080'
