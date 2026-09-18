# Kyrios
Repositório para arquivar as planilhas de controle de integrante do Kyrios

## Jogo de corrida top-vision (C#)

Além das planilhas, este repositório também guarda um jogo de corrida em C#
visto de cima ("top-vision"), rodando no terminal.

### Estrutura

- `src/Kyrios.Core` — biblioteca com toda a lógica do jogo (sem depender de
  console): vetores, física do carro, pista, checkpoints/voltas, IA dos
  adversários e a simulação da corrida.
- `src/Kyrios.ConsoleGame` — aplicativo de console: renderiza a pista em
  ASCII, lê o teclado e roda o loop do jogo.
- `tests/Kyrios.Core.Tests` — testes automatizados (xUnit) da lógica do jogo.

### Como jogar

Pré-requisito: [.NET SDK 8.0+](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/Kyrios.ConsoleGame
```

Controles:

- Setas ou `WASD` — acelerar, frear/ré e virar
- `Espaço` — freio de mão
- `Esc` — sair a qualquer momento
- `R` — correr de novo (na tela de resultado)

Complete 3 voltas na pista em anel, passando pelos 3 checkpoints na ordem
certa antes de cruzar a linha de chegada, competindo contra carros
controlados por IA.

### Modo sem interface (headless)

Útil para rodar em CI/terminais sem TTY, ou só para ver a IA correndo:

```bash
dotnet run --project src/Kyrios.ConsoleGame -- --simulate --ai 4 --laps 3 --seed 42
```

### Testes

```bash
dotnet test
```
