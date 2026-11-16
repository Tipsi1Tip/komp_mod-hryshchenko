namespace TvLine.Sim;

public abstract class Element(string name)
{
    public string Name { get; } = name;

    public double TNext { get; protected set; } = double.PositiveInfinity;

    public double TCurr { get; set; }

    public long Quantity { get; protected set; }

    public Router? Router { get; set; }

    public string LastAction { get; protected set; } = "";

    public virtual void InAct() { }

    public abstract void OutAct();

    public virtual void DoStatistics(double dt) { }

    public virtual void ResetStatistics() => Quantity = 0;

    protected void Pass() => Router?.Select().InAct();
}
