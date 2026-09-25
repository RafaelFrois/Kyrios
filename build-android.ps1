# Gera a versão Android (Play Store) do MegRace no Windows.
# Resultado: dist\android\MegRace.aab (enviar pra Play Store) e dist\android\MegRace.apk (instalar direto no celular).
#
# Requisitos: SDK .NET 10 com o workload android (dotnet workload install android) — o Visual Studio 2026 com a carga
# ".NET Multi-platform App UI" já traz tudo (SDK Android + JDK). Se o SDK/JDK estiverem em outro lugar, defina
# ANDROID_HOME e JAVA_HOME.
#
# Assinatura (Play Store): defina MEGRACE_KEYSTORE (arquivo .keystore), MEGRACE_KEY_ALIAS e MEGRACE_KEYSTORE_PASSWORD.
# Sem isso o app sai assinado com a chave de debug — serve pra testar no celular, mas a Play Store recusa.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\Kyrios.Game.Android\Kyrios.Game.Android.csproj'
$out = Join-Path $root 'dist\android'

$arguments = @('publish', $project, '-c', 'Release')
if ($env:ANDROID_HOME) { $arguments += "-p:AndroidSdkDirectory=$env:ANDROID_HOME" }
if ($env:JAVA_HOME) { $arguments += "-p:JavaSdkDirectory=$env:JAVA_HOME" }
if ($env:MEGRACE_KEYSTORE) {
    $alias = if ($env:MEGRACE_KEY_ALIAS) { $env:MEGRACE_KEY_ALIAS } else { 'megrace' }
    $keyPassVar = if ($env:MEGRACE_KEY_PASSWORD) { 'MEGRACE_KEY_PASSWORD' } else { 'MEGRACE_KEYSTORE_PASSWORD' }
    $arguments += @('-p:AndroidKeyStore=true', "-p:AndroidSigningKeyStore=$env:MEGRACE_KEYSTORE", "-p:AndroidSigningKeyAlias=$alias",
        '-p:AndroidSigningStorePass=env:MEGRACE_KEYSTORE_PASSWORD', "-p:AndroidSigningKeyPass=env:$keyPassVar")
    Write-Host "Assinando com $env:MEGRACE_KEYSTORE"
} else {
    Write-Warning 'Sem MEGRACE_KEYSTORE: assinado com a chave de debug (só pra testar, a Play Store recusa).'
}

Remove-Item -Recurse -Force $out, (Join-Path $root 'src\Kyrios.Game.Android\bin\Release') -ErrorAction SilentlyContinue
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Falha no build Android.' }
New-Item -ItemType Directory -Force $out | Out-Null
$publish = Join-Path $root 'src\Kyrios.Game.Android\bin\Release\net10.0-android\publish'
Copy-Item (Join-Path $publish 'com.rafaelfrois.megrace-Signed.aab') (Join-Path $out 'MegRace.aab')
Copy-Item (Join-Path $publish 'com.rafaelfrois.megrace-Signed.apk') (Join-Path $out 'MegRace.apk')
Write-Host "Pronto: $out\MegRace.aab e $out\MegRace.apk"
