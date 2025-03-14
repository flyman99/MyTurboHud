namespace Turbo.Plugins
{
    public interface IAvoid: IActor
    {
        IAvoidRule AvoidRule { get; }
    }
}