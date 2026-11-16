namespace TvLine.Sim;

public sealed class Dispose(string name) : Element(name)
{
    public override void InAct()
    {
        Quantity++;
        LastAction = $"Вихід в {Name}";
    }

    public override void OutAct() { }
}
