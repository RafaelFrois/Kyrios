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
  IA dos adversários e a simulação da corrida. É compartilhada pelas duas
  versões abaixo.
- `src/Kyrios.Game` — **jogo gráfico** (MonoGame): abre uma janela, desenha a
  pista e os carros, e pode ser publicado como executável standalone.
- `src/Kyrios.ConsoleGame` — versão de terminal (ASCII), útil quando não dá
  pra abrir uma janela (ex: SSH sem X11) ou pra rodar em CI.
- `tests/Kyrios.Core.Tests` — testes automatizados (xUnit) da lógica do jogo.

### Pré-requisito

[.NET SDK 8.0+](https://dotnet.microsoft.com/download).

### Como jogar (versão gráfica, recomendada)

```bash
dotnet run --project src/Kyrios.Game
```

Isso compila e abre a janela do jogo direto. Controles:

- Setas ou `WASD` — acelerar, frear/ré e virar
- `Espaço` — freio de mão
- `Esc` — sair a qualquer momento
- `R` — correr de novo (na tela de resultado)

Complete 3 voltas na pista em anel, passando pelos 3 checkpoints (marcados em
amarelo) na ordem certa antes de cruzar a linha de chegada quadriculada,
competindo contra carros controlados por IA.

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

Mesmos controles da versão gráfica. Também tem um modo sem interface
(headless), útil para CI ou terminais sem teclado interativo:

```bash
dotnet run --project src/Kyrios.ConsoleGame -- --simulate --ai 4 --laps 3 --seed 42
```

### Testes

```bash
dotnet test
```
