namespace Kyrios.Core;

public enum DriverKind
{
    Human,
    Ai,
}

/// <summary>Um participante da corrida: o carro em si, mais quem o controla e seu status na prova.</summary>
public sealed class RaceEntrant
{
    public Car Car { get; }
    public DriverKind Kind { get; }
    public AIDriver? Driver { get; }

    public bool Finished { get; internal set; }
    public float? FinishTime { get; internal set; }
    public int? FinishPlace { get; internal set; }

    /// <summary>true se foi eliminado (modo <see cref="RaceMode.Elimination"/>) antes de conseguir vencer.</summary>
    public bool Eliminated { get; internal set; }

    /// <summary>Pontuação acumulada (modo <see cref="RaceMode.TimeAttack"/>).</summary>
    public float Score { get; internal set; }

    public RaceEntrant(Car car, DriverKind kind, AIDriver? driver = null)
    {
        if (kind == DriverKind.Ai && driver is null)
        {
            throw new ArgumentException("Um participante controlado por IA precisa de um AIDriver.", nameof(driver));
        }

        Car = car;
        Kind = kind;
        Driver = driver;
    }
}
