namespace Turbo.Plugins
{
    public interface IMarker
    {
        string Id { get; }

        ISnoActor SnoActor { get; }
        ISnoQuest SnoQuest { get; }
        uint WorldId { get; }
        IWorldCoordinate FloorCoordinate { get; }
        string Name { get; }

        uint TextureSno { get; }
        int TextureFrameIndex { get; }

        bool IsPylon { get; }
        bool IsShrine { get; }
        bool IsPoolOfReflection { get; }
        bool IsHealingWell { get; }
        bool IsUsed { get; } // does not work for shrines
    }
}