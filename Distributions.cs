namespace TvLine.Sim;

public interface IDistribution
{
    double Mean { get; }
    double Next(Random rng);
}

public sealed record Deterministic(double D) : IDistribution
{
    public double Mean => D;
    public double Next(Random rng) => D;
}

public sealed record Uniform : IDistribution
{
    public Uniform(double d, double s)
    {
        if (s < 0 || s > d) throw new ArgumentOutOfRangeException(nameof(s), "unif(d, s) має бути невід'ємною: 0 <= s <= d");
        D = d;
        S = s;
    }

    public double D { get; }
    public double S { get; }
    public static Uniform Range(double a, double b) => new((a + b) / 2, (b - a) / 2);
    public double Mean => D;
    public double Next(Random rng) => D + S * (2.0 * rng.NextDouble() - 1.0);
}

public sealed record Exponential(double D) : IDistribution
{
    public double Mean => D;
    public double Next(Random rng) => -D * Math.Log(1.0 - rng.NextDouble());
}
