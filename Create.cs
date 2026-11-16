namespace TvLine.Sim;

public sealed class Create : Element
{
    private readonly IDistribution delay;
    private readonly Random rng;

    public Create(string name, IDistribution delay, int batch, Random rng) : base(name)
    {
        this.delay = delay;
        this.rng = rng;
        Batch = batch;
        TNext = delay.Next(rng);
    }

    public int Batch { get; }

    public override void OutAct()
    {
        TNext = TCurr + delay.Next(rng);
        for (int i = 0; i < Batch; i++)
        {
            Quantity++;
            Pass();
        }
        LastAction = Batch == 1 ? $"Надходження в {Router!.Routes[0].Target.Name}"
                                : $"Надходження {Batch} ТВ в {Router!.Routes[0].Target.Name}";
    }
}
