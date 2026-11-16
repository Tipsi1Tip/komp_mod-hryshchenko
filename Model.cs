using System.Globalization;

namespace TvLine.Sim;

public sealed class Model
{
    private readonly List<Element> elements = [];

    public IReadOnlyList<Element> Elements => elements;
    public double Time { get; private set; }
    public double StatisticsStart { get; private set; }
    public double ObservedTime => Time - StatisticsStart;
    public long EventCount { get; private set; }

    public TextWriter? Protocol { get; set; }
    public Func<string>? StateFormatter { get; set; }

    public Model Add(Element e)
    {
        elements.Add(e);
        return this;
    }

    public void Simulate(double tMod)
    {
        while (true)
        {
            double tNext = double.PositiveInfinity;
            Element? nearest = null;
            foreach (var e in elements)
            {
                if (e.TNext < tNext)
                {
                    tNext = e.TNext;
                    nearest = e;
                }
            }
            if (nearest == null || tNext > tMod) break;

            foreach (var e in elements) e.DoStatistics(tNext - Time);

            Time = tNext;
            foreach (var e in elements) e.TCurr = Time;

            Act(nearest);
            foreach (var e in elements)
            {
                if (e.TNext == Time) Act(e);
            }
        }
        if (double.IsFinite(tMod) && tMod > Time)
        {
            foreach (var e in elements) e.DoStatistics(tMod - Time);
            Time = tMod;
            foreach (var e in elements) e.TCurr = Time;
        }
    }

    private void Act(Element e)
    {
        e.OutAct();
        EventCount++;
        if (Protocol == null) return;
        string state = StateFormatter?.Invoke() ?? "";
        Protocol.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{Time,10:F3} | {e.LastAction,-34} | {state}"));
    }

    public void ResetStatistics()
    {
        foreach (var e in elements) e.ResetStatistics();
        StatisticsStart = Time;
    }
}
