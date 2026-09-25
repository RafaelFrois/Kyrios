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
}

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
