using System.Globalization;
using TvLine.Sim;

namespace TvLine.Network;

public sealed class TvLineNetwork
{
    public TvLineNetwork(ModelParameters p, int seed)
    {
        p.Validate();
        Parameters = p;
        Rng = new Random(seed);

        Source = new Create("Надходження", Dist(p.ArrivalMin, p.ArrivalMax), p.Batch, Rng);
        Check = new Process("СМО1", p.Controllers, Dist(p.CheckMin, p.CheckMax), Rng);
        Adjust = new Process("СМО2", p.Adjusters, Dist(p.AdjustMin, p.AdjustMax), Rng);
        Packing = new Dispose("Упакування");
        if (p.PreCheck)
        {
            IDistribution pre = p.Exponential ? new Exponential(p.PreCheckTime) : new Deterministic(p.PreCheckTime);
            PreCheck = new Process("СМО0", p.PreCheckers, pre, Rng);
        }

        if (PreCheck != null)
        {
            Source.Router = Router.To(PreCheck, Rng);
            PreCheck.Router = Router.To(Check, Rng);
        }
        else
        {
            Source.Router = Router.To(Check, Rng);
        }
        double pa = p.PAdjustEffective;
        Check.Router = new Router(Rng).Add(Packing, 1 - pa).Add(Adjust, pa);
        Adjust.Router = new Router(Rng).Add(Packing, p.PAdjustToPack).Add(Check, 1 - p.PAdjustToPack);

        Model.Add(Adjust).Add(Check);
        if (PreCheck != null) Model.Add(PreCheck);
        Model.Add(Source).Add(Packing);
    }

    public ModelParameters Parameters { get; }
    public Random Rng { get; }
    public Model Model { get; } = new();
    public Create Source { get; }
    public Process? PreCheck { get; }
    public Process Check { get; }
    public Process Adjust { get; }
    public Dispose Packing { get; }

    private IDistribution Dist(double a, double b)
    {
        if (Parameters.Deterministic) return new Deterministic((a + b) / 2);
        if (Parameters.Exponential) return new Exponential((a + b) / 2);
        return Uniform.Range(a, b);
    }

    public int InSystem => Check.Content + Adjust.Content + (PreCheck?.Content ?? 0);

    public double ContentTime => Check.BusyTime + Check.QueueTime + Adjust.BusyTime + Adjust.QueueTime
                                 + (PreCheck == null ? 0 : PreCheck.BusyTime + PreCheck.QueueTime);

    public TvLineNetwork Preload(int k, int m)
    {
        Check.Preload(k);
        Adjust.Preload(m);
        return this;
    }

    public SimulationResult Run(double tMod, double tWarmup = 0)
    {
        if (tWarmup > 0)
        {
            Model.Simulate(tWarmup);
            Model.ResetStatistics();
        }
        Model.Simulate(tMod);
        return Collect();
    }

    public SimulationResult Collect()
    {
        double t = Model.ObservedTime;
        long packed = Packing.Quantity;
        return new SimulationResult
        {
            ObservedTime = t,
            Packed = packed,
            MeanTime = packed == 0 ? 0 : ContentTime / packed,
            MeanInSystem = ContentTime / t,
            RhoCheck = Check.BusyTime / (t * Check.Channels),
            RhoAdjust = Adjust.BusyTime / (t * Adjust.Channels),
            RhoPre = PreCheck == null ? 0 : PreCheck.BusyTime / (t * PreCheck.Channels),
            QueueCheck = Check.QueueTime / t,
            QueueAdjust = Adjust.QueueTime / t,
            QueuePre = PreCheck == null ? 0 : PreCheck.QueueTime / t,
            WaitCheck = Check.Arrivals == 0 ? 0 : Check.QueueTime / Check.Arrivals,
            WaitAdjust = Adjust.Arrivals == 0 ? 0 : Adjust.QueueTime / Adjust.Arrivals,
            Throughput = packed / t * 60.0,
            ChecksPerTv = packed == 0 ? 0 : (double)Check.Quantity / packed,
        };
    }

    public void EnableProtocol(TextWriter writer)
    {
        Model.Protocol = writer;
        Model.StateFormatter = () => string.Create(CultureInfo.InvariantCulture,
            $"{Check.Busy} {Check.Queue} | {Adjust.Busy} {Adjust.Queue} | {Source.Quantity} {Check.Quantity} {Adjust.Quantity} {Packing.Quantity}");
    }
}
