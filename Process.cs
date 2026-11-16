namespace TvLine.Sim;

public sealed class Process : Element
{
    private readonly IDistribution delay;
    private readonly Random rng;
    private readonly double[] tDevice;

    public Process(string name, int channels, IDistribution delay, Random rng,
                   int maxQueue = int.MaxValue) : base(name)
    {
        if (channels < 1) throw new ArgumentOutOfRangeException(nameof(channels));
        if (maxQueue < 0) throw new ArgumentOutOfRangeException(nameof(maxQueue));
        this.delay = delay;
        this.rng = rng;
        MaxQueue = maxQueue;
        tDevice = new double[channels];
        Array.Fill(tDevice, double.PositiveInfinity);
    }

    public int Channels => tDevice.Length;
    public int MaxQueue { get; }

    public int Busy { get; private set; }

    public int Queue { get; private set; }

    public int Content => Busy + Queue;

    public long Arrivals { get; private set; }
    public long Failures { get; private set; }
    public double BusyTime { get; private set; }
    public double QueueTime { get; private set; }

    public override void InAct()
    {
        Arrivals++;
        int free = Array.IndexOf(tDevice, double.PositiveInfinity);
        if (free >= 0)
        {
            Start(free);
        }
        else if (Queue < MaxQueue)
        {
            Queue++;
        }
        else
        {
            Failures++;
        }
    }

    public override void OutAct()
    {
        for (int r = 0; r < tDevice.Length; r++)
        {
            if (tDevice[r] != TCurr) continue;
            Quantity++;
            Busy--;
            tDevice[r] = double.PositiveInfinity;
            if (Queue > 0)
            {
                Queue--;
                Start(r);
            }
            Pass();
        }
        TNext = tDevice.Min();
        LastAction = $"Кінець обслуговування {Name}";
    }

    private void Start(int r)
    {
        Busy++;
        tDevice[r] = TCurr + delay.Next(rng);
        TNext = Math.Min(TNext, tDevice[r]);
    }

    public void Preload(int k)
    {
        for (int i = 0; i < k; i++) InAct();
        ResetStatistics();
    }

    public override void DoStatistics(double dt)
    {
        BusyTime += Busy * dt;
        QueueTime += Queue * dt;
    }

    public override void ResetStatistics()
    {
        base.ResetStatistics();
        Arrivals = 0;
        Failures = 0;
        BusyTime = 0;
        QueueTime = 0;
    }
}
