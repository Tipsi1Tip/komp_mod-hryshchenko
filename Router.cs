namespace TvLine.Sim;

public sealed class Router(Random rng)
{
    private readonly List<(Element Target, double P)> routes = [];

    public IReadOnlyList<(Element Target, double P)> Routes => routes;

    public static Router To(Element target, Random rng) => new Router(rng).Add(target, 1.0);

    public Router Add(Element target, double p)
    {
        if (p < 0 || p > 1) throw new ArgumentOutOfRangeException(nameof(p));
        if (p > 0) routes.Add((target, p));
        if (routes.Sum(r => r.P) > 1 + 1e-9) throw new InvalidOperationException("Сума ймовірностей маршрутів більша за 1");
        return this;
    }

    public Element Select()
    {
        if (routes.Count == 1) return routes[0].Target;
        double r = rng.NextDouble();
        double acc = 0;
        foreach (var (target, p) in routes)
        {
            acc += p;
            if (r < acc) return target;
        }
        return routes[^1].Target;
    }
}
