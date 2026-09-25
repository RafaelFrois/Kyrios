namespace Kyrios.Game;

/// <summary>Qualidade gráfica escolhida nas configurações do celular.</summary>
public enum GraphicsQuality
{
    /// <summary>Decide sozinho pelo aparelho e baixa se o jogo começar a engasgar.</summary>
    Auto,
    Low,
    Medium,
    High,
}

/// <summary>Conversão entre as preferências do celular salvas em texto no save (estáveis entre versões e legíveis) e
/// os tipos usados pelo jogo. Valor desconhecido (save editado, versão futura) volta pro padrão.</summary>
public static class MobileSettings
{
    public static ControlScheme Scheme(string code) => code switch
    {
        "joystick" => ControlScheme.Joystick,
        "tilt" => ControlScheme.Tilt,
        _ => ControlScheme.Buttons,
    };

    public static string Code(ControlScheme scheme) => scheme switch
    {
        ControlScheme.Joystick => "joystick",
        ControlScheme.Tilt => "tilt",
        _ => "buttons",
    };

    public static ControlSize Size(string code) => code switch
    {
        "small" => ControlSize.Small,
        "large" => ControlSize.Large,
        _ => ControlSize.Medium,
    };

    public static string Code(ControlSize size) => size switch
    {
        ControlSize.Small => "small",
        ControlSize.Large => "large",
        _ => "medium",
    };

    public static GraphicsQuality Quality(string code) => code switch
    {
        "low" => GraphicsQuality.Low,
        "medium" => GraphicsQuality.Medium,
        "high" => GraphicsQuality.High,
        _ => GraphicsQuality.Auto,
    };

    public static string Code(GraphicsQuality quality) => quality switch
    {
        GraphicsQuality.Low => "low",
        GraphicsQuality.Medium => "medium",
        GraphicsQuality.High => "high",
        _ => "auto",
    };
}
