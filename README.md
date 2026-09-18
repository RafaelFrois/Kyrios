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
- `Shift` — turbo (consome o medidor no canto superior direito, que enche com
  o tempo e mais rápido em cada checkpoint/volta)
- `Espaço` — freio de mão
- `Esc` — sair a qualquer momento
- `R` — correr de novo (mesmo modo) / `M` — trocar de modo (na tela de resultado)

Depois da tela inicial, escolha o modo:

- **Corrida** — o clássico: complete 3 voltas na pista em anel, passando
  pelos 3 checkpoints (marcados em amarelo) na ordem certa, na frente dos
  adversários de IA.
- **Eliminação** — a cada volta completada por qualquer carro, o último
  colocado é eliminado, até sobrar um único campeão. Muda o ritmo do jogo
  inteiro: cada volta é uma faca no pescoço.

Seu melhor tempo de volta/corrida (modo Corrida) e seu retrospecto de vitórias
(modo Eliminação) ficam salvos localmente entre sessões — o jogo avisa na tela
de resultado quando você bate um recorde pessoal.

Os carros também colidem de verdade entre si e reagem diferente a bater de
frente numa parede (ricocheteia, perde velocidade) ou só raspar de lado
(quase não perde nada) — e a IA "gruda" na disputa (rubber-banding): fica mais
dura com quem está na frente e mais fácil com quem está atrás, pra manter a
corrida disputada até o fim.

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

# ou modo eliminação
dotnet run --project src/Kyrios.ConsoleGame -- --simulate --ai 5 --seed 42 --mode elimination
```

### Testes

```bash
dotnet test
```
