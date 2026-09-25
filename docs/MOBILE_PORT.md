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

## Plano (fases)

1. **Análise** (este documento).
2. **Projeto Android** compilando e empacotando (APK/AAB), com `AndroidPlatform` mínimo.
3. **Input abstrato** + esquemas de toque (BOTÕES, JOYSTICK, INCLINAR) + tamanho dos botões, aceleração automática e feedback ao tocar.
4. **UI mobile**: menu principal, seleção de skins e pistas com ← → e vitrine, conquistas em cartões com rolagem por arraste, configurações em página de toque, HUD de corrida, pausa, resultado, área segura e aviso "GIRE O DISPOSITIVO".
5. **Vibração** (com opção), **qualidade gráfica** (BAIXA/MÉDIA/ALTA/AUTO com detecção do aparelho e ajuste em tempo real), **save v2**, **ciclo de vida** (pausa ao perder o foco, recriação de texturas) e **carregamento** com tela própria.
6. **Testes**: layout em 16:9 a 21:9, tablets 4:3 e 16:10, dobráveis, área segura; toque (vários dedos, toque rápido, segurar, arrastar); save (fechar, forçar, atualizar, corromper); FPS 30/60/90/120; casos extremos. Validação visual com um **simulador de celular** (a mesma `GameRoot` rodando no desktop com a plataforma "celular").
7. **Build de produção**: ícone adaptável, nome, splash, AAB + APK assinados, sem debug, checklist da Play Store.

### Observação sobre o pedido
O pedido fala em "quatro carros" na Corrida Mortal, mas o jogo tem **10 carros** desde a
versão desktop (e na web). A versão mobile mantém os 10, porque é a mesma gameplay.

### Limitação deste ambiente
Aqui não existe aparelho Android nem emulador (sem KVM). O APK/AAB é compilado e
verificado estruturalmente, e as telas são validadas no simulador de celular (o mesmo
código, com a plataforma "celular"). O teste final em aparelho real está no checklist.
