namespace TvLine.Network;

public sealed record ModelParameters
{
    public double ArrivalMin { get; init; } = 3.5;
    public double ArrivalMax { get; init; } = 7.5;

    public int Batch { get; init; } = 1;

    public int Controllers { get; init; } = 2;
    public double CheckMin { get; init; } = 6;
    public double CheckMax { get; init; } = 12;

    public double PAdjust { get; init; } = 0.15;

    public int Adjusters { get; init; } = 1;
    public double AdjustMin { get; init; } = 20;
    public double AdjustMax { get; init; } = 40;

    public double PAdjustToPack { get; init; } = 0.0;

    public bool PreCheck { get; init; }
    public int PreCheckers { get; init; } = 1;
    public double PreCheckTime { get; init; } = 5;
    public double PreCheckReduction { get; init; } = 0.1;

    public bool Exponential { get; init; }

    public bool Deterministic { get; init; }

    public static ModelParameters Base => new();

    public void Validate()
    {
        if (ArrivalMin <= 0 || ArrivalMax < ArrivalMin) throw new ArgumentException("Некоректний інтервал надходження");
        if (CheckMin <= 0 || CheckMax < CheckMin) throw new ArgumentException("Некоректний час перевірки");
        if (AdjustMin <= 0 || AdjustMax < AdjustMin) throw new ArgumentException("Некоректний час налаштування");
        if (Batch < 1 || Controllers < 1 || Adjusters < 1 || PreCheckers < 1) throw new ArgumentException("Кількості мають бути додатними");
        if (PAdjust is < 0 or > 1 || PAdjustToPack is < 0 or > 1) throw new ArgumentException("Ймовірності мають бути в [0; 1]");
        if (PAdjustEffective * (1 - PAdjustToPack) >= 1) throw new ArgumentException("Телевізори ніколи не залишають етап");
    }

    public double MeanArrival => (ArrivalMin + ArrivalMax) / 2;
    public double MeanCheck => (CheckMin + CheckMax) / 2;
    public double MeanAdjust => (AdjustMin + AdjustMax) / 2;

    public double PAdjustEffective => PreCheck ? Math.Max(0, PAdjust - PreCheckReduction) : PAdjust;

    public double CheckFlow => Batch / MeanArrival / (1 - PAdjustEffective * (1 - PAdjustToPack));

    public double RhoCheck => CheckFlow * MeanCheck / Controllers;

    public double RhoAdjust => CheckFlow * PAdjustEffective * MeanAdjust / Adjusters;

    public double RhoPre => PreCheck ? Batch / MeanArrival * PreCheckTime / PreCheckers : 0;

    public double ChecksPerTv => 1 / (1 - PAdjustEffective * (1 - PAdjustToPack));
}
