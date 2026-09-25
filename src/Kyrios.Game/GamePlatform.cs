namespace Kyrios.Game;

/// <summary>
/// Onde o save fica guardado. Desktop: arquivo JSON (com cópia de segurança). Navegador: armazenamento local.
/// Nenhuma operação pode lançar exceção — se o armazenamento falhar, o jogo continua com o progresso em memória.
/// </summary>
public interface ISaveStore
{
    /// <summary>Os textos salvos, do mais novo pro mais antigo (save principal, depois a cópia de segurança).</summary>
    IEnumerable<string> ReadCandidates();

    void Write(string json);
}

/// <summary>
/// O que muda entre a versão desktop (.exe) e a versão web (navegador/Poki). O jogo inteiro é o mesmo código;
/// só estes pontos consultam a plataforma. O padrão é o desktop — a versão web troca por <see cref="Current"/>
/// antes de criar o <see cref="GameRoot"/>.
/// </summary>
public class GamePlatform
{
    public static GamePlatform Current { get; set; } = new();

    /// <summary>Roda dentro de um navegador (sem "sair do jogo", tela cheia controlada pelo site, etc.).</summary>
    public virtual bool IsWeb => false;

    /// <summary>Pode fechar o próprio programa (botão SAIR, ESC no menu principal).</summary>
    public virtual bool CanQuit => true;

    /// <summary>O jogo controla a própria tela cheia (opção nas configurações e F11).</summary>
    public virtual bool ControlsFullscreen => true;

    /// <summary>A plataforma usa o passo fixo do framework (desktop). Na web o laço é do navegador e o jogo
    /// mantém o passo fixo da simulação sozinho.</summary>
    public virtual bool UsesFrameworkFixedTimeStep => true;

    public virtual ISaveStore SaveStore => new FileSaveStore(FileSaveStore.DefaultPath);

    /// <summary>Hora local do jogador (conquistas de madrugada, dias jogados).</summary>
    public virtual DateTime LocalNow => DateTime.Now;

    /// <summary>Tamanho desejado da área de desenho em pixels reais (null = o framework decide).</summary>
    public virtual (int Width, int Height)? DesiredBackBufferSize => null;

    /// <summary>Pixels da área de desenho por pixel informado pelo mouse/toque (densidade da tela no navegador).</summary>
    public virtual float PointerScale => 1f;

    /// <summary>O aparelho é de toque (celular/tablet): o jogo já começa mostrando os controles de toque.</summary>
    public virtual bool PrefersTouch => false;

    /// <summary>A logo já decodificada pela plataforma (null = o jogo decodifica o PNG embutido). No navegador o
    /// decodificador nativo é dezenas de vezes mais rápido que decodificar em C# dentro do WebAssembly.</summary>
    public virtual DecodedImage DecodedLogo => null;

    /// <summary>O jogo terminou de carregar e está pronto pra interação.</summary>
    public virtual void LoadingFinished()
    {
    }

    /// <summary>O jogador começou (ou voltou) a jogar de fato. Chamado só na troca de estado, nunca duplicado.</summary>
    public virtual void GameplayStart()
    {
    }

    /// <summary>O jogo parou de ser jogado (pausa, fim de partida, menus).</summary>
    public virtual void GameplayStop()
    {
    }

    /// <summary>Intervalo comercial numa parada natural, logo antes de voltar pra pista. A plataforma decide se
    /// mostra algo; <paramref name="onFinished"/> é chamado exatamente uma vez quando o jogo pode seguir.</summary>
    public virtual void CommercialBreak(Action onFinished) => onFinished();

    // ---------- Celular / tablet (app nativo) ----------

    /// <summary>App de celular/tablet: telas com alvos de toque grandes, controles de corrida medidos em dp,
    /// configurações de controle, vibração e qualidade gráfica. Desktop e web ficam com as telas de sempre.</summary>
    public virtual bool IsMobile => false;

    /// <summary>Faixas da tela que não podem ter nada importante (notch, furo da câmera, cantos arredondados,
    /// barras do sistema), em pixels da área de desenho. A cena inteira fica dentro do que sobra.</summary>
    public virtual SafeInsets SafeInsets => SafeInsets.None;

    /// <summary>Pixels da área de desenho por dp (densidade da tela) — pra medir botões em tamanho físico.</summary>
    public virtual float PixelsPerDp => 1f;

    /// <summary>Lê os dedos na tela (em pixels informados pela plataforma, antes do <see cref="PointerScale"/>).
    /// O padrão é o painel de toque do framework; o simulador de celular injeta toques daqui.</summary>
    public virtual void ReadTouches(List<TouchPoint> touches)
    {
        foreach (Microsoft.Xna.Framework.Input.Touch.TouchLocation touch in Microsoft.Xna.Framework.Input.Touch.TouchPanel.GetState())
        {
            if (touch.State is Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Pressed or Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Moved)
            {
                touches.Add(new TouchPoint(touch.Id, touch.Position, touch.State == Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Pressed));
            }
        }
    }

    /// <summary>O aparelho sabe vibrar (a opção VIBRAÇÃO só aparece se sim).</summary>
    public virtual bool CanVibrate => false;

    /// <summary>Vibração curta de feedback (batida, checkpoint, eliminação, vitória, desbloqueio...).</summary>
    public virtual void Vibrate(Haptic kind)
    {
    }

    /// <summary>O aparelho tem sensor de movimento pro controle por inclinação.</summary>
    public virtual bool HasTiltSensor => false;

    /// <summary>Inclinação atual do aparelho segurado deitado, como um volante: -1 (toda pra esquerda) a 1 (toda
    /// pra direita), já com zona morta. null = sem leitura agora.</summary>
    public virtual float? ReadTilt() => null;

    /// <summary>Liga/desliga o sensor de inclinação (só fica ligado quando o esquema INCLINAR está em uso — bateria).</summary>
    public virtual void SetTiltSensorEnabled(bool enabled)
    {
    }

    /// <summary>O botão/gesto "voltar" do sistema foi usado desde a última consulta (cada uso vale uma vez).</summary>
    public virtual bool ConsumeBackRequest() => false;

    /// <summary>O app foi interrompido desde a última consulta: ligação, tela bloqueada, barra de notificações,
    /// troca de app. O jogo pausa a corrida e grava o progresso.</summary>
    public virtual bool ConsumeInterruption() => false;

    /// <summary>O sistema pediu pra liberar memória desde a última consulta (o jogo esvazia caches).</summary>
    public virtual bool ConsumeLowMemoryWarning() => false;

    /// <summary>"Voltar" no menu principal de um celular: o app vai pro fundo (sem perder nada), como qualquer app.</summary>
    public virtual void LeaveToBackground()
    {
    }

    /// <summary>Potência estimada do aparelho (memória, núcleos, tela), usada pela qualidade gráfica automática.</summary>
    public virtual DeviceTier DeviceTier => DeviceTier.High;

    /// <summary>Pede ao sistema que a tela atualize nessa taxa (60 ou 30): em telas de 90/120 Hz evita trepidação e
    /// gasta menos bateria, já que o jogo não desenha mais que isso.</summary>
    public virtual void SetPreferredFrameRate(int framesPerSecond)
    {
    }

    /// <summary>O jogo mostra a própria tela de carregamento (a web já tem a dela em HTML; o desktop carrega rápido).</summary>
    public virtual bool ShowsLoadingScreen => false;

    /// <summary>Quantas pistas desenhadas ficam guardadas na memória de vídeo (cada uma ~3 MB).</summary>
    public virtual int SceneryCacheSize => 4;

    /// <summary>O contexto gráfico pode ser perdido e recriado com o app aberto (Android): as texturas feitas em código
    /// precisam ser refeitas quando isso acontece.</summary>
    public virtual bool CanLoseGraphicsContext => false;

    /// <summary>O app está indo pro fundo agora (pode ser fechado pelo sistema a qualquer momento depois disso).
    /// Quem assina grava o que ainda não foi gravado.</summary>
    public event Action Suspending;

    protected void RaiseSuspending() => Suspending?.Invoke();
}

/// <summary>Um dedo na tela: identificador estável enquanto o dedo não sai, posição e se encostou agora.</summary>
public readonly record struct TouchPoint(int Id, Microsoft.Xna.Framework.Vector2 Position, bool JustPressed);

/// <summary>Faixas proibidas nas bordas da tela, em pixels.</summary>
public readonly record struct SafeInsets(int Left, int Top, int Right, int Bottom)
{
    public static SafeInsets None => default;
}

/// <summary>Tipos de vibração — cada plataforma decide duração e intensidade.</summary>
public enum Haptic
{
    /// <summary>Toque em botão (bem leve).</summary>
    Tap,
    Checkpoint,
    Collision,
    Elimination,
    PlayerEliminated,
    Victory,
    Unlock,
    Achievement,
}

public enum DeviceTier
{
    Low,
    Medium,
    High,
}

/// <summary>Imagem pronta pra virar textura: tamanho original, tamanho da textura (potência de dois, o resto
/// transparente) e todos os níveis de mipmap em RGBA com alfa pré-multiplicado, um depois do outro.</summary>
public sealed record DecodedImage(int Width, int Height, int TextureWidth, int TextureHeight, byte[] MipChain);

/// <summary>Save em arquivo (desktop): gravação atômica num temporário + cópia de segurança da versão anterior.</summary>
public sealed class FileSaveStore(string path) : ISaveStore
{
    public static string DefaultPath
    {
        get
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegRace");
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }

            return Path.Combine(dir, "records.json");
        }
    }

    public IEnumerable<string> ReadCandidates()
    {
        foreach (string candidate in new[] { path, path + ".bak" })
        {
            string text = null;
            try
            {
                if (File.Exists(candidate))
                {
                    text = File.ReadAllText(candidate);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }

            if (text is not null)
            {
                yield return text;
            }
        }
    }

    /// <summary>Escreve num arquivo temporário e só então troca pelo save de verdade (a versão anterior vira a cópia
    /// de segurança). Se o jogo fechar no meio, sobra pelo menos um save inteiro.</summary>
    public void Write(string json)
    {
        try
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }

            File.Move(temp, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
