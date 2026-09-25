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
- `F11` — alterna tela cheia / janela (o jogo já abre em tela cheia)

No menu, escolha e equipe o carro em **SKINS** e o cenário em **PISTAS** (as
duas telas mostram a coleção inteira numa grade, com o que falta pra liberar
cada item); depois é só ir em **JOGAR**, escolher o modo e a partida começa
depois da contagem 3-2-1. Também dá pra acompanhar as **CONQUISTAS** e ajustar
som, tela cheia (ligada por padrão) e idioma (português ou inglês) em **CONFIGURAÇÕES**. Conquistas, skins e pistas liberadas
aparecem num aviso no topo da tela.

Modos:

- **Corrida Mortal** — 10 carros (9 rivais + você); a cada volta completada por qualquer carro, o
  último colocado é eliminado, até sobrar um só. Seja o último de pé.
- **Contra o Relógio** — o relógio só desce; checkpoints dão tempo e pontos,
  batidas tiram tempo. Acaba quando o tempo zera.

Pistas (29): todas usam **exatamente o mesmo circuito** (curvas, checkpoints,
largada e colisões iguais) — muda só o mundo em volta: Autódromo, Praia,
Floresta, Montanha, Estádio, Cidade à Noite, Deserto, Neve, Supermercado, Fundo
do Mar, Sala de Estar, Escritório, Parque de Diversões, Cidade Neon, Vulcão,
Mesa da Cozinha, Setup Gamer, Lixão, Fábrica, Quarto de Criança, Planeta
Alienígena, Acima das Nuvens, Estação Espacial, Placa-Mãe, Caderno, Mesa de
Sinuca, Bolo de Aniversário, Noite Assombrada — e uma secreta. Cada uma esconde
um detalhe na pista.

Skins (74): veículos, animais, comidas, objetos e coisas absurdas — da galinha
e do vaso sanitário ao buraco negro e à galinha de kart do logo.

Progressão de longo prazo: as primeiras skins e pistas saem rápido; o resto pede
habilidade (voltas rápidas, vitórias sem bater, de virada, de ponta a ponta...),
exploração (segredos das pistas, combinações de skin + pista), sequências,
combinações de metas e algumas descobertas escondidas. São 220 conquistas
(fáceis, médias, difíceis, muito difíceis, raras, idiotas e secretas). Tudo fica
salvo localmente (`%AppData%/MegRace/records.json` no Windows, com uma cópia de
segurança `records.json.bak` da gravação anterior).

Para adicionar conteúdo, basta uma linha no catálogo correspondente:
`CarSkins.cs` + `CarSkins.*.cs` (skins e seus desenhos), `TrackThemes.cs` +
`TrackSceneries*.cs` (pistas) e `Achievements.cs` (conquistas); os requisitos
prontos ficam em `UnlockRequirements.cs`.

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

### Versão web (navegador / Poki)

O mesmo jogo roda no navegador (WebAssembly + WebGL, via [KNI](https://github.com/kniEngine/kni)) — mesmo
código, mesmo conteúdo, mesmo save, mesma física. Detalhes, testes e checklist da Poki em
[`docs/WEB_PORT.md`](docs/WEB_PORT.md).

```bash
./build-web.sh          # Linux/macOS
./build-web.ps1         # Windows (PowerShell)
```

Gera `dist/web/` (pasta pronta pra servir) e `dist/MegRace-web.zip` (index.html na raiz — o formato do Poki
Inspector). Pra testar localmente sirva a pasta por HTTP (ex.: `cd dist/web && python3 -m http.server 8080`) —
abrir o `index.html` direto do disco não funciona em navegador nenhum.

**Vercel:** o repositório já vem configurado (`vercel.json` + `vercel-build.sh`, que instala o .NET 8 e publica
o projeto web). Basta importar o repositório com o preset "Other" e fazer o deploy — sem mudar nenhuma opção.

Na web: toque em celular/tablet (botões na corrida), save no navegador (sincronizado pela Poki), sem botão SAIR
e com a tela cheia controlada pelo site.

### Versão de terminal (ASCII)

```bash
dotnet run --project src/Kyrios.ConsoleGame
```

Joga a Corrida Mortal no terminal. Também tem um modo sem interface
(headless), útil para CI ou terminais sem teclado interativo:

```bash
dotnet run --project src/Kyrios.ConsoleGame -- --simulate --ai 9 --seed 42

# ou Contra o Relógio
dotnet run --project src/Kyrios.ConsoleGame -- --simulate --ai 1 --seed 42 --mode timeattack
```

### Testes

```bash
dotnet test
```
