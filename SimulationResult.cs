namespace TvLine.Network;

public sealed record SimulationResult
{
    public double ObservedTime { get; init; }
    public long Packed { get; init; }
    public double MeanTime { get; init; }
    public double MeanInSystem { get; init; }
    public double RhoCheck { get; init; }
    public double RhoAdjust { get; init; }
    public double RhoPre { get; init; }
    public double QueueCheck { get; init; }
    public double QueueAdjust { get; init; }
    public double QueuePre { get; init; }
    public double WaitCheck { get; init; }
    public double WaitAdjust { get; init; }
    public double Throughput { get; init; }
    public double ChecksPerTv { get; init; }
}
