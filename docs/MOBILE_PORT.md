# MegRace no celular (Android / Play Store) — análise e plano

A versão mobile **não é outro jogo**: é o mesmo código C# do desktop e da web, com uma
"casca" de plataforma para Android. Esta branch (`claude/megrace-mobile`) não mexe na
`main` nem no deploy web (Vercel/Poki).

## Fase 1 — análise do projeto

### Onde cada sistema mora hoje

| Sistema | Onde está | Situação para o mobile |
|---|---|---|
| Engine | MonoGame 3.8.5.1 (DesktopGL) / KNI (web) | **MonoGame.Framework.Android 3.8.5.1**: a mesma versão do desktop, então a mesma API |
| Física, colisões, turbo | `Kyrios.Core/Car.cs` (sem MonoGame) | compartilhada, sem mudança |
| IA | `Kyrios.Core/AIDriver.cs` | compartilhada, sem mudança |
| Regras dos modos, checkpoints, pontuação, relógio | `Kyrios.Core/RaceSimulation.cs` | compartilhadas, sem mudança |
| Passo de tempo | acumulador de 1/60 s em `GameRoot.Race` (criado na versão web) | compartilhado: física/IA/relógio iguais a 30, 60, 90 ou 120 fps |
| Telas (menu, modos, coleção, conquistas, configurações, HUD, pausa, resultado) | `GameRoot.*.cs` — layouts escritos à mão numa resolução lógica fixa 1268x564 | lógica compartilhada; os **layouts** precisam de uma variante de toque (alvos de 44 dp ou mais, texto maior) |
| Câmera | a pista inteira sempre visível, escalada com letterbox (`BuildScreenTransform`) | compartilhada; precisa respeitar a **área segura** (notch, furo da câmera, cantos arredondados) |
| Input | `GameInput` (teclado, mouse, gamepad, toque como mouse) + `GameRoot.Touch` (botões da web) | falta uma **camada abstrata**: várias fontes (teclado, gamepad, toque, inclinação) → uma única `CarInput` |
| Áudio | 100% procedural (`Synth`, `Soundtrack`) → `SoundEffect` | funciona igual (OpenAL no Android); precisa pausar/retomar com o ciclo de vida do app |
| Save | `SaveData` (JSON por *source generator*, sem reflexão) + `ISaveStore` (arquivo no desktop, `localStorage` na web), versão 1 | arquivo no armazenamento interno do app (gravação atômica + `.bak`); **versão 2** com as preferências novas do mobile |
| Progressão, skins, pistas, conquistas, estatísticas, recordes | `Progression`, `Unlockables`, `CarSkins.*`, `TrackThemes`, `Achievements` | compartilhados, sem mudança: **74 skins, 29 pistas, 220 conquistas, 2 idiomas** |
| Cenários | `TrackSceneries.*` desenhados uma vez num `RenderTarget2D` (cache LRU de 4) | compartilhados; no Android o contexto OpenGL pode ser recriado, e as texturas feitas em código precisam ser refeitas |
| Plataforma | `GamePlatform` (padrão desktop; `WebPlatform` na web) | ganha um `AndroidPlatform` e novas capacidades: área segura, densidade, vibração, inclinação, botão voltar, interrupções e perfil do aparelho |

### Só do desktop
Janela, tela cheia/F11, botão SAIR, dicas de teclado e save em `%APPDATA%`.

### Só da web
Poki SDK, `localStorage`, carregador em HTML/JS, liberação do áudio no primeiro toque,
canvas × `devicePixelRatio` e aviso de retrato.

### O que o mobile precisa e ainda não existe
Área segura, densidade real (dp), vibração, controle por inclinação, botão voltar do
Android, pausa ao perder o foco (ligação, bloqueio de tela, barra de notificações, troca
de app), recriação de texturas após perda do contexto OpenGL, qualidade gráfica adaptável,
limite de FPS (bateria/calor), esquemas de controle, layouts de toque, tela de carregamento
e ícone/splash/AAB.

## Decisões técnicas

| Decisão | Motivo |
|---|---|
| **MonoGame.Framework.Android 3.8.5.1** | É a mesma engine e a mesma versão do desktop. O código compartilhado compila sem `#if`. |
| **.NET 10 (`net10.0-android`, API 36)** | A Play Store sobe o *target* mínimo todo ano (API 35 em agosto de 2025, API 36 em seguida). O Android 16 (API 36) é o mais recente, e o workload Android do .NET 8 para na API 34 e já não recebe suporte. O pacote do MonoGame (`net8.0-android34`) funciona num app `net10.0-android`. |
| Projeto `src/Kyrios.Game.Android` compila os mesmos `.cs` de `Kyrios.Game` | É a mesma técnica da web: **um jogo, várias cascas**. O projeto Android fica fora do `Kyrios.sln` porque exige o SDK .NET 10 + workload `android`; o resto continua em .NET 8. |
| Paisagem travada (`sensorLandscape`) + `appCategory="game"` | No Android 16 os apps que miram a API 36 perdem o travamento de orientação em telas grandes. Jogos são a exceção, via `appCategory`. |
| Cena inteira dentro da área segura | Nada do jogo fica atrás do notch ou do furo da câmera. A sobra vira fundo do cenário. |
| Controles de toque medidos em **dp** (tamanho físico) | O mesmo tamanho de dedo em celular e tablet. Em telas 16:9 e tablets a pista sobe e os controles ficam na faixa livre de baixo, sem cobrir a pista. |
| Simulação fixa em 60 passos/s; FPS limitado a 60 (ou 30 na qualidade BAIXA) | A física é a mesma em qualquer taxa de quadros. Desenhar a 90/120 Hz só repetiria quadros e gastaria bateria. Por isso a tela é pedida a 60 Hz. |
| Save: arquivo interno + Auto Backup do Android | Sobrevive a fechar o app, a um fechamento forçado e a atualizações. O Auto Backup restaura o save em outro aparelho ou numa reinstalação. |
| Sem permissões perigosas | Só `VIBRATE`, que é normal e concedida automaticamente. Sem internet, sem anúncios e sem coleta de dados. |

## Arquitetura: um jogo, várias plataformas

```
Teclado ─┐
Gamepad ─┼─► fontes de controle ─► CarControls (soma/limita) ─► CarInput ─► RaceSimulation (Core)
Toque ───┤     (TouchControls: BOTÕES / JOYSTICK / INCLINAR)
Inclinação┘

GameRoot (telas, HUD, áudio, save, progressão)  ◄── compartilhado por desktop, web e Android
   │  consulta só GamePlatform.Current
   ├─ GamePlatform        (desktop: janela, arquivo, F11, SAIR)
   ├─ WebPlatform         (navegador: localStorage, Poki, canvas)
   └─ AndroidPlatform     (área segura, dp, vibração, inclinação, voltar, ciclo de vida, perfil do aparelho)
```

## Fases 2-7 — o que foi feito

### Estrutura

```
src/Kyrios.Core              física, IA, regras (sem MonoGame) — igual pra todas as plataformas
src/Kyrios.Game              o jogo inteiro (telas, HUD, áudio, save, progressão, input) + casca desktop
  GamePlatform.cs            o que muda por plataforma (padrão = desktop)
  TouchControls.cs           controles de toque (lógica pura: layout em dp, esquemas, leitura dos dedos)
  FixedStepClock.cs          passo fixo de 1/60 s da simulação
  MobileSettings.cs          preferências do celular (save v2)
  GameRoot.Mobile.cs         qualidade gráfica, vibração, interrupções, memória, contexto gráfico
  GameRoot.*Mobile.cs        layouts de toque (coleção, conquistas, configurações)
src/Kyrios.Game.Web          casca web (KNI/Blazor) — compila os fontes de Kyrios.Game
src/Kyrios.Game.Android      casca Android — compila os fontes de Kyrios.Game
  MainActivity.cs            tela cheia imersiva, área segura, voltar, foco, memória
  AndroidPlatform.cs         save interno, vibração, inclinação, perfil do aparelho, 60 Hz, tela ligada
tools/Kyrios.MobilePreview   simulador de celular (desenvolvimento)
```

Nenhuma regra de jogo tem `#if` de plataforma. As telas de toque são escolhidas por `GamePlatform.IsMobile`, e o
desktop e a web continuam com as telas de sempre (conferido por captura de tela).

### Input (Fase 3)
- **Camada abstrata:** teclado, controle, toque e inclinação viram um único `CarInput`. `TouchControls.Combine`
  soma os valores e limita a -1..1; turbo e freio de mão valem se qualquer fonte pedir. Menus usam ações
  (Confirmar, Voltar...), e o "voltar" do Android entra como Voltar/Pausa.
- **Esquemas:**
  - **BOTÕES** (padrão): ◄ ► / ACEL, FREIO-RÉ e TURBO.
  - **JOYSTICK**: o carro vira para onde o polegar aponta e acelera conforme o quanto é empurrado; FREIO e TURBO ficam à direita.
  - **INCLINAR**: gira o aparelho como um volante; FREIO à esquerda, ACEL e TURBO à direita, e um volantinho mostra a leitura do sensor.
- **Tamanho:** PEQUENO, MÉDIO ou GRANDE. Os botões são medidos em dp (o mesmo tamanho físico em qualquer tela), com mínimo de 48 dp e área de toque 1,5x maior que o desenho.
- **Aceleração automática:** opção de acessibilidade em que o jogador só vira e freia.
- **Feedback e toque:** o botão afunda e acende quando pressionado, e o anel do TURBO mostra o combustível. Vários dedos funcionam juntos, o dedo pode deslizar entre botões, e um dedo novo conta como toque mesmo com outro já apoiado.
- **Modo toque:** o controle físico ou o teclado escondem os botões de toque; tocar na tela mostra de novo.

### Tela e UI (Fase 4)
- **Área segura:** a cena inteira fica dentro dela (notch, furo da câmera, bordas curvas, cantos arredondados).
- **Posição da cena na corrida:** ela sobe só o necessário para os controles caberem abaixo da pista.
  - Em 16:9, tablets e dobráveis nada da pista fica coberto.
  - Em 20:9 e 21:9 não há sobra, então os controles ficam translúcidos sobre os cantos.
- **Menu principal:** JOGAR grande e grade 2x2. O painel "pronto pra correr" é tocável. "Voltar" manda o app para o fundo.
- **Skins e pistas:** vitrine grande, ◄ ► e deslize, faixa com os vizinhos. O painel mostra nome, categoria, estado, requisito com barra de progresso e as estatísticas do item (partidas, vitórias, recorde ou melhor volta). O botão EQUIPAR é grande.
- **Conquistas:** cartões grandes em 2 colunas e abas maiores. A rolagem acompanha o dedo, continua com inércia ao soltar e a lista é recortada na própria área.
- **Configurações:** página inteira de toque com 8 opções. Música e efeitos têm barra arrastável e botão de mudo; idioma; vibração; controles; tamanho; aceleração automática; qualidade (com "AGORA: MÉDIA" no AUTO).
- **HUD:**
  - Corrida Mortal: posição, volta, carros restantes e a próxima eliminação.
  - Contra o Relógio: o tempo grande, os pontos, o recorde e **quanto o próximo checkpoint dá (+X S)**.
- **Pausa e resultado:** botões grandes. A pausa é um botão grande na faixa de cima, e pausado não há input de corrida.
- **Aviso de desbloqueio:** mais curto no celular.
- **Retrato** (tela dividida, janela solta): "GIRE O DISPOSITIVO".

### Sistema (Fase 5)
- **Qualidade AUTO/BAIXA/MÉDIA/ALTA:**
  - O AUTO parte do perfil do aparelho (memória, núcleos, versão do Android, 32/64 bits).
  - Durante a corrida, se o FPS ficar abaixo de 80% da meta por 4 s, a qualidade cai um nível e nunca sobe sozinha na mesma sessão.
  - BAIXA desenha a 30 fps, com 40% do clima e 50% das partículas. MÉDIA usa 70% e 80%. ALTA fica completa.
  - O jogo pede 60 Hz à tela: em telas de 90/120 Hz isso evita trepidação e gasta menos bateria.
- **Física, IA e relógio:** passos fixos de 1/60 s (`FixedStepClock`) em qualquer taxa de quadros; está testado a 30, 60, 90, 120 e 144 fps.
- **Vibração** (opção VIBRAÇÃO): batida, checkpoint/volta, eliminação (mais forte quando é o jogador; assistindo depois de eliminado não vibra), vitória, desbloqueio, conquista, recorde e toques leves nos botões principais.
- **Ciclo de vida:**
  - Perder o foco (ligação, tela bloqueada, barra de notificações, troca de app) pausa a corrida e grava o save.
  - Ir para o fundo grava de novo. O áudio é pausado pelo próprio MonoGame.
  - Na volta, o jogo não "pula" o tempo perdido.
  - Memória baixa libera as pistas guardadas.
  - Se o contexto OpenGL for recriado, as texturas feitas em código são refeitas.
- **Tela ligada:** fica acesa durante a corrida (quem joga com controle ou inclinação não toca na tela) e volta ao normal nos menus.
- **Memória:** no máximo 3 pistas desenhadas na memória de vídeo (2 em aparelho fraco), cada uma com ~3 MB.
- **Carregamento:**
  - Enquanto o jogo inicia aparece a splash do sistema (a galinha no fundo escuro).
  - Depois, a abertura do jogo carrega o resto em etapas (áudio sintetizado, primeira pista, catálogos), com barra de progresso.
- **Áudio:** no app nativo o som não precisa esperar o primeiro toque (isso é regra de navegador), então a música do menu começa na abertura, como no desktop.
- **Save versão 2:** guarda esquema de controle, tamanho, aceleração automática, vibração e qualidade.
  - Um save versão 1 abre com todo o progresso e as preferências novas no padrão.
  - Valores inválidos voltam ao padrão, e um save de versão futura abre sem fingir ser antigo.
  - A gravação é atômica, com `.bak`, no armazenamento interno.
  - Entra no **backup automático do Android** (reinstalação ou aparelho novo).

### Testes (Fase 6)
- **42 testes novos** em `tests/Kyrios.Game.Tests/MobileTests.cs` (total do projeto: 58 + 94 = 152, todos passando):
  - **Layout:** 10 aparelhos (16:9, 18:9, 19,5:9 com notch, 20:9 com furo, 21:9, 720p, 480p antigo, tablet 16:10, tablet 4:3, dobrável) × 3 esquemas × 3 tamanhos. Tudo dentro da área segura, sem sobreposição, alvos ≥ 48 dp e controles na metade de baixo.
  - **Toque:** vários dedos juntos, toque rápido (só naquele quadro), segurar por 300 quadros, deslizar de ◄ para ►, deslizar de ACEL para FREIO, toques longe dos controles, polegar um pouco fora do botão.
  - **Joystick:** direção relativa ao carro, limite do raio, o dedo continua dono do joystick fora da zona, e um segundo dedo não rouba.
  - **Inclinação** (e sem leitura do sensor) e **aceleração automática**.
  - **A mesma corrida a 30, 60, 90, 120 e 144 fps** (posição, pontos e tempo idênticos); passo fixo após engasgos e com valores inválidos.
  - **Save:**
    - v1 → v2, com preferências inválidas e com versão futura.
    - Ciclo desbloquear skin/pista/conquista/recorde → fechar → abrir.
    - Fechamento forçado no meio da gravação (sobra `.tmp` pela metade).
    - Arquivo principal corrompido, recuperado pelo `.bak`.
- **Simulador de celular** (`tools/Kyrios.MobilePreview`): o mesmo jogo no desktop com a plataforma "celular". Roteiros de toque passaram por:
  - todas as telas;
  - corrida com BOTÕES, JOYSTICK e INCLINAR;
  - interrupção (pausa sozinho e grava);
  - "voltar" (pausa e retoma; no menu, app para o fundo);
  - eliminação → resultado;
  - capturas em 10 aparelhos e em retrato.

  O roteiro usa `expect-state` para falhar se a tela não for a esperada.
- **Desktop e web:**
  - O desktop foi conferido por captura: telas iguais.
  - A web foi testada no Chromium como celular (toques via CDP): carrega, joga, pausa e mostra o aviso de retrato, sem erros no console.
  - Na web só os botões de toque da corrida mudaram (agora medidos em dp, com anel de turbo); menus e HUD são os de sempre. A `main` e o deploy da Vercel não foram tocados.

### Build de produção (Fase 7)
- `./build-android.sh` / `build-android.ps1` → `dist/android/MegRace.aab` (Play Store) e `MegRace.apk` (instalação direta).
- **Configuração:**
  - Release, sem símbolos de debug, AOT com perfil (abre mais rápido), R8 e trimming parcial.
  - `targetSdk` 36, `minSdk` 24 (Android 7.0+).
  - ARM64, ARM32 e x86_64; bibliotecas nativas alinhadas a 16 KB (exigência da Play Store).
- **Tamanho:** o AAB tem ~19 MB, e o download na Play Store fica em **~6,5 MB por aparelho**, porque cada um baixa só a sua arquitetura.
- **Ícone e splash:**
  - Ícone adaptável (galinha sobre o gradiente do título) e ícones antigos, redondos e quadrados.
  - Splash do Android 12+ e splash antiga.
  - Imagens da loja em `docs/store/`: ícone 512, imagem de destaque 1024x500, 5 capturas de celular (16:9) e 4 de tablet.
- **Identificação:** o id do app é `com.rafaelfrois.megrace`; **ele não pode mudar depois do primeiro envio**. A versão é `ApplicationVersion` (subir a cada envio) / `ApplicationDisplayVersion`, no `.csproj`.
- **Permissões:** só `VIBRATE`. Sem internet, anúncios, rastreamento ou coleta de dados.

### Assinatura
A Play Store usa **Assinatura de apps do Google Play**: você assina o AAB com uma *chave de upload*, e o Google
assina o app final.

1. Crie a chave de upload uma vez:
   ```bash
   keytool -genkeypair -v -keystore megrace-upload.keystore -storetype PKCS12 -alias megrace -keyalg RSA -keysize 4096 -validity 10000
   ```
2. **Guarde o arquivo e a senha em dois lugares seguros.** Nunca coloque no repositório (o `.gitignore` já bloqueia
   `*.keystore`/`*.jks`). Se a chave de upload se perder, dá para pedir a troca ao suporte da Play.
3. Gere o AAB assinado:
   ```bash
   export MEGRACE_KEYSTORE=/caminho/megrace-upload.keystore MEGRACE_KEY_ALIAS=megrace MEGRACE_KEYSTORE_PASSWORD='...'
   ./build-android.sh
   ```

### Checklist da Play Store
- [x] AAB assinado, `targetSdk` 36, 64 bits, páginas de 16 KB
- [x] Paisagem travada (`sensorLandscape`) e `appCategory="game"` (respeitado em tablets no Android 16)
- [x] Tela cheia imersiva, borda a borda e área segura
- [x] "Voltar" (botão e gesto preditivo do Android 13+) tratado pelo jogo
- [x] Sem permissões perigosas, sem internet e sem SDKs de terceiros
- [x] Ícone 512x512, imagem de destaque 1024x500 e capturas (`docs/store/`)
- [x] Política de privacidade: texto pronto em [`docs/PRIVACIDADE.md`](PRIVACIDADE.md) (hospedar numa URL pública, ex.: GitHub ou o site da Vercel)
- [ ] **Segurança dos dados** (Play Console): "não coleta nem compartilha dados"
- [ ] **Classificação indicativa** (questionário IARC): corridas de carrinhos em desenho, sem sangue, sem compras e sem anúncios
- [ ] **Público-alvo:** se incluir menores de 13 anos, a Play pede as regras de "Famílias". O jogo já as cumpre (sem anúncios e sem dados), mas precisa declarar.
- [ ] **Teste fechado:** contas pessoais novas precisam de 12 testadores por 14 dias antes da produção
- [ ] **Teste em aparelho real** (abaixo)

### Precisa de verificação em aparelho real
Aqui não havia aparelho Android nem emulador (sem KVM). O APK/AAB foi compilado, assinado e inspecionado: manifesto,
alinhamento de 16 KB e tamanho por aparelho. Todas as telas e fluxos rodaram no simulador com o mesmo código. Em aparelho,
conferir:

1. O app abre, a splash aparece e o jogo não dá tela preta. Testar em Android 7, 10, 13 e 15/16, se possível.
2. Toque com vários dedos, voltar/gesto, notch/furo real e cantos arredondados.
3. Vibração e sensor de inclinação (direção e sensibilidade).
4. Pausa ao receber ligação, bloquear a tela, puxar as notificações e trocar de app; o jogo retoma sem perder nada.
5. FPS e temperatura num aparelho de entrada (a qualidade AUTO deve baixar sozinha se precisar).
6. Save: jogar, fechar forçado pelas Configurações do Android, abrir; desinstalar e reinstalar (backup do Google).

### Observação sobre o pedido
O pedido fala em "quatro carros" na Corrida Mortal, mas o jogo tem **10 carros** desde a
versão desktop (e na web). A versão mobile mantém os 10, porque é a mesma gameplay.
