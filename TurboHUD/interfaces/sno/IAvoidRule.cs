namespace Turbo.Plugins
{
    public interface IAvoidRule
    {
        AvoidType Type { get; }
        AvoidWeight Weight { get; }
        ISnoActor SnoActor { get; }
        float Radius { get; }
        bool Dead { get; }
    }
}