#!/usr/bin/env bash
# Gera a versão Android (Play Store) do MegRace.
# Resultado: dist/android/MegRace.aab (enviar pra Play Store) e dist/android/MegRace.apk (instalar direto no celular).
#
# Requisitos: SDK .NET 10 com o workload android (dotnet workload install android), Android SDK com a plataforma 36
# e o build-tools, e JDK 17 ou 21. Se o SDK/JDK não estiverem nos lugares padrão, defina ANDROID_HOME e JAVA_HOME.
#
# Assinatura (Play Store): defina MEGRACE_KEYSTORE (arquivo .keystore), MEGRACE_KEY_ALIAS e MEGRACE_KEYSTORE_PASSWORD
# (e MEGRACE_KEY_PASSWORD, se a senha da chave for diferente). Sem isso o app sai assinado com a chave de debug — serve
# pra testar no celular, mas a Play Store recusa.
set -euo pipefail
root="$(cd "$(dirname "$0")" && pwd)"
project="$root/src/Kyrios.Game.Android/Kyrios.Game.Android.csproj"
out="$root/dist/android"

args=(-c Release)
[ -n "${ANDROID_HOME:-}" ] && args+=("-p:AndroidSdkDirectory=$ANDROID_HOME")
[ -n "${JAVA_HOME:-}" ] && args+=("-p:JavaSdkDirectory=$JAVA_HOME")
if [ -n "${MEGRACE_KEYSTORE:-}" ]; then
  args+=(-p:AndroidKeyStore=true "-p:AndroidSigningKeyStore=$MEGRACE_KEYSTORE" "-p:AndroidSigningKeyAlias=${MEGRACE_KEY_ALIAS:-megrace}"
         "-p:AndroidSigningStorePass=env:MEGRACE_KEYSTORE_PASSWORD" "-p:AndroidSigningKeyPass=env:${MEGRACE_KEY_PASSWORD:+MEGRACE_KEY_PASSWORD}${MEGRACE_KEY_PASSWORD:-MEGRACE_KEYSTORE_PASSWORD}")
  echo "Assinando com $MEGRACE_KEYSTORE"
else
  echo "AVISO: sem MEGRACE_KEYSTORE — assinado com a chave de debug (só pra testar, a Play Store recusa)."
fi

rm -rf "$out" "$root/src/Kyrios.Game.Android/bin/Release"
dotnet publish "$project" "${args[@]}"
mkdir -p "$out"
publish="$root/src/Kyrios.Game.Android/bin/Release/net10.0-android/publish"
cp "$publish/com.rafaelfrois.megrace-Signed.aab" "$out/MegRace.aab"
cp "$publish/com.rafaelfrois.megrace-Signed.apk" "$out/MegRace.apk"
echo "Pronto: $out/MegRace.aab ($(du -h "$out/MegRace.aab" | cut -f1)) e $out/MegRace.apk ($(du -h "$out/MegRace.apk" | cut -f1))"
