namespace Turbo.Plugins
{
    public interface IShrine : IClickableActor
    {
        ShrineType Type { get; }

        bool IsShrine { get; }
        bool IsPylon { get; }
        bool IsHealingWell { get; }
        bool IsPoolOfReflection { get; }
    }
}