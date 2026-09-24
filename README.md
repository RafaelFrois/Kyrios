# Kyrios
Repositório para arquivar as planilhas de controle de integrante do Kyrios

## MegRace — jogo de corrida top-vision (C#)

Além das planilhas, este repositório também guarda o **MegRace**, um jogo de
corrida em C# visto de cima ("top-vision"): uma versão **gráfica** (janela
própria, feita com a engine [MonoGame](https://www.monogame.net/), com pista,
carros e telas em estilo pixel art) e uma versão de **terminal** (ASCII) que
roda a mesma lógica.

### Estrutura

- `src/Kyrios.Core` — biblioteca com toda a lógica do jogo (sem depender de
  janela nem de console): vetores, física do carro, pista, checkpoints/voltas,
  IA dos adversários e a simulação da partida. É compartilhada pelas duas
  versões abaixo.
- `src/Kyrios.Game` — **jogo gráfico** (MonoGame): menus, pistas/cenários,
  skins, conquistas, progressão e salvamento; pode ser publicado como
  executável standalone.
- `src/Kyrios.ConsoleGame` — versão de terminal (ASCII), útil quando não dá
  pra abrir uma janela (ex: SSH sem X11) ou pra rodar em CI.
- `tests/` — testes automatizados (xUnit) da lógica (`Kyrios.Core.Tests`) e da
  progressão/catálogos (`Kyrios.Game.Tests`).

### Pré-requisito

[.NET SDK 8.0+](https://dotnet.microsoft.com/download).

### Como jogar (versão gráfica, recomendada)

```bash
dotnet run --project src/Kyrios.Game
```

Controles (teclado ou controle):

- Setas ou `WASD` — acelerar, frear/ré e virar (controle: gatilhos + analógico)
- `Shift` — turbo (controle: RB ou B)
- `Espaço` — freio de mão (controle: A)
- `Esc` / `P` — pausar (controle: START)
- `F11` — tela cheia

No menu, escolha e equipe o carro em **SKINS** e o cenário em **PISTAS**;
depois é só ir em **JOGAR**, escolher o modo e a partida começa. Também dá pra
acompanhar as **CONQUISTAS** e ajustar o som em **CONFIGURAÇÕES**. Conquistas,
skins e pistas liberadas aparecem num aviso no topo da tela.

Modos:

- **Corrida Mortal** — 4 carros; a cada volta completada por qualquer carro, o
  último colocado é eliminado, até sobrar um só. Seja o último de pé.
- **Contra o Relógio** — o relógio só desce; checkpoints dão tempo e pontos,
  batidas tiram tempo. Acaba quando o tempo zera.

Pistas: todas usam **exatamente o mesmo circuito** (curvas, checkpoints,
largada e colisões iguais) — muda só o cenário: Autódromo, Praia, Floresta,
Cidade à Noite, Deserto, Neve, Supermercado, Cidade Neon, Vulcão, Mesa da
Cozinha e Quarto de Criança. Cada uma esconde um detalhe secreto na pista.

Progressão: skins, pistas e conquistas (fáceis, médias, difíceis, raras,
idiotas e secretas) são liberadas pelo que você faz nos dois modos. Tudo fica
salvo localmente (`%AppData%/MegRace/records.json` no Windows).

Para adicionar conteúdo, basta uma linha no catálogo correspondente:
`CarSkins.cs` (skins), `TrackThemes.cs` + `TrackSceneries*.cs` (pistas) e
`Achievements.cs` (conquistas); os requisitos prontos ficam em
`UnlockRequirements.cs`.

### Gerar um executável (sem precisar do .NET instalado)

Para distribuir um arquivo único que roda sem precisar instalar o .NET SDK na
máquina de destino, publique para o sistema operacional desejado:

```bash
# flags comuns a todas as plataformas (deixam o executável bem menor)
FLAGS="-p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:InvariantGlobalization=true -p:PublishTrimmed=true -p:DebugType=none"

# Windows
dotnet publish src/Kyrios.Game -c Release -r win-x64 --self-contained true $FLAGS -o dist/windows

# macOS (Apple Silicon)
dotnet publish src/Kyrios.Game -c Release -r osx-arm64 --self-contained true $FLAGS -o dist/macos

# macOS (Intel)
dotnet publish src/Kyrios.Game -c Release -r osx-x64 --self-contained true $FLAGS -o dist/macos

# Linux
dotnet publish src/Kyrios.Game -c Release -r linux-x64 --self-contained true $FLAGS -o dist/linux
```

O resultado fica em `dist/<sistema>/`: um executável (`MegRace.exe` no Windows,
`MegRace` no macOS/Linux) junto de algumas bibliotecas nativas (SDL2, OpenAL) —
copie a pasta inteira, não só o executável.

### Versão de terminal (ASCII)

```bash
dotnet run --project src/Kyrios.ConsoleGame
```

Joga a Corrida Mortal no terminal. Também tem um modo sem interface
(headless), útil para CI ou terminais sem teclado interativo:

```bash
dotnet run --project src/Kyrios.ConsoleGame -- --simulate --ai 4 --seed 42

# ou Contra o Relógio
dotnet run --project src/Kyrios.ConsoleGame -- --simulate --ai 1 --seed 42 --mode timeattack
```

### Testes

```bash
dotnet test
```
