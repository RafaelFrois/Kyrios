# MegRace na Web (Poki) — análise e plano da conversão

Este documento registra a análise do projeto (Fases 1 e 2), as decisões técnicas e o
estado de cada fase. A versão web **não é outro jogo**: é o mesmo código C# rodando no
navegador. Só a "casca" de plataforma (janela, arquivo de save, SDK da Poki, toque)
muda.

## Fase 1 — o projeto hoje

| Item | Situação atual |
|---|---|
| Engine | MonoGame **3.8.5.1**, plataforma DesktopGL (SDL2 + OpenGL + OpenAL) |
| Linguagem / runtime | C# 12, **.NET 8** |
| Build | `dotnet build` / `dotnet publish` (exe único win-x64, trimmed) |
| Projetos | `Kyrios.Core` (simulação pura, sem MonoGame), `Kyrios.Game` (o jogo), `Kyrios.ConsoleGame` (versão de terminal antiga), 2 projetos de teste (99 testes) |
| Cenas | máquina de estados em `GameRoot` (`State`: Splash, MainMenu, ModeSelect, SkinSelect, TrackSelect, Achievements, Racing, Paused, Settings, Results) — sem arquivos de cena |
| Física | própria, em `Kyrios.Core/Car.cs` (aceleração, atrito, derrapagem, colisão com parede/carros/obstáculos) |
| IA | `Kyrios.Core/AIDriver.cs` — segue a linha central, *rubber-banding*, estilos variados, usa turbo |
| Corrida Mortal | `RaceSimulation` modo Elimination: **10 carros**, o último cai a cada volta do líder, vence o último de pé |
| Contra o Relógio | `RaceSimulation` modo TimeAttack: 20 s iniciais, checkpoints e voltas dão tempo (com decaimento) e pontos, batidas tiram tempo, obstáculos móveis |
| Checkpoints | 9 checkpoints + linha de chegada, definidos em `TrackFactory` (mesmo traçado em todas as pistas) |
| Input | `GameInput`: teclado (setas/WASD, Shift, Espaço, Esc/P, Enter), mouse (menus, sliders), gamepad (XInput) |
| Áudio | **100% procedural**: `Synth` + `Soundtrack` geram PCM na inicialização → `SoundEffect`; músicas em loop, motor com pitch, ~20 efeitos. Nenhum arquivo de áudio |
| Gráficos | `SpriteBatch` 2D. Tudo desenhado por código (carros em `CarPainter`, cenários em `TrackSceneries.*` cacheados em `RenderTarget2D`, luzes num passe aditivo). Único arquivo de imagem: `logo.png` (210 KB) |
| UI | desenhada por código com fonte bitmap própria (`PixelFont`, 5x7) — resolução lógica fixa 1268x564 escalada com letterbox |
| Save | `SaveData` → JSON (System.Text.Json com *source generator*) em `%APPDATA%/MegRace/records.json`, gravação atômica + `.bak` |
| Progressão | `Progression.RecordRace` atualiza ~70 estatísticas; `Unlockables` libera conquistas → skins → pistas |
| Conteúdo | **74 skins**, **29 pistas** (mesmo traçado), **220 conquistas** (47 secretas), 2 idiomas (PT/EN) |
| Dependências | só `MonoGame.Framework.DesktopGL` (traz SDL2.dll, openal.dll). Nenhum plugin, API externa ou serviço online |

## Fase 2 — o que funciona na web e o que precisa de adaptação

Abordagem escolhida: **KNI** (fork do MonoGame/XNA, `nkast.Kni.Platform.Blazor.GL`
4.3.9001), que roda o mesmo código C# no navegador via Blazor WebAssembly + WebGL.
Reescrever em JavaScript foi descartado (seria outro jogo).

**Prova técnica (feita antes de mexer no projeto):** o código atual inteiro compilou
contra o KNI web com **um único erro** (nome de parâmetro) e rodou no Chromium:
menu, corrida com 10 carros, IA, HUD e teclado a 60 fps, sem erros no console.
Tamanho com o jogo inteiro: **2,9 MB (brotli)** — a meta da Poki é < 5 MB iniciais.

### Reaproveitado sem mudança
Simulação, física, IA, checkpoints, pontuação, temporizador, todas as skins, cenários,
conquistas, requisitos, estatísticas, idiomas, UI, efeitos e o sintetizador de áudio.

### Precisa de adaptação (com a solução)

| Sistema | Problema na web | Solução |
|---|---|---|
| Passo de tempo | o passo fixo do KNI faz *espera ativa* no navegador (trava a CPU em telas 120/144 Hz); sem ele, a física dependeria do FPS | acumulador próprio: a corrida avança sempre em passos de 1/60 s (idêntico ao desktop, que já roda a 60 passos/s) |
| Save | não existe disco; o sistema de arquivos do WASM some ao fechar a aba | `localStorage` (cópia principal + backup), com versão e migração; a Poki sincroniza `localStorage` na nuvem sozinha |
| Relógio local | sem dados de fuso no WASM, `DateTime.Now` seria UTC (quebraria as conquistas de madrugada) | hora local vem do navegador |
| Perfil gráfico | perfil "Reach" (WebGL 1) recusa a logo com mipmaps fora de potência de dois | a logo é colocada numa textura potência de dois ao carregar (mantém WebGL 1 compatível) |
| Tela / resolução | canvas nasce 800x480 e o KNI ignora a densidade de pixels (texto ilegível em celular) | canvas = tamanho real × `devicePixelRatio` (com teto), refeito a cada mudança |
| Tela cheia | a Poki controla a tela cheia no site dela | opção "tela cheia" e F11 escondidos na web |
| Sair do jogo | não existe "fechar" num navegador | botão SAIR e ESC-sair escondidos na web |
| Áudio | navegadores bloqueiam som antes da 1ª interação | o contexto de áudio é liberado no 1º toque/tecla; suspenso em anúncios e com a aba oculta |
| Toque | o jogo só tem teclado/mouse/controle | toques viram cliques nos menus; na corrida, botões grandes de toque (virar, acelerar, frear, turbo, pausa) |
| Orientação | jogo é paisagem (2,25:1) | aviso "gire o aparelho" em retrato no celular |
| Poki SDK | — | ponte JS com fallback (funciona mesmo com bloqueador): `gameLoadingFinished`, `gameplayStart/Stop` sem duplicar, `commercialBreak` antes de cada corrida |
| Carregamento | tela padrão do KNI/Blazor | tela própria com a logo, barra e porcentagem |
| Tamanho | ICU, Bootstrap, arquivos .gz/.pdb/duplicados | globalização invariante, só arquivos .br + descompressão no navegador |

### Observação sobre o pedido
O pedido cita "quatro carros" na Corrida Mortal, mas o jogo atual tem **10 carros**
(3 no mínimo pra IA disputar). Seguindo a regra de não mudar o gameplay, a versão web
mantém os 10.

## Estrutura

- `src/Kyrios.Game` — o jogo (compartilhado). Os pontos que dependem da plataforma
  passam por `GamePlatform` (save, hora local, eventos da Poki, recursos disponíveis);
  o padrão é o comportamento desktop.
- `src/Kyrios.Game.Web` — projeto KNI BlazorGL que compila os mesmos arquivos do jogo
  e adiciona só a casca web (página, ponte da Poki, save no navegador).
- A versão desktop (`.exe`) continua sendo gerada como antes.
