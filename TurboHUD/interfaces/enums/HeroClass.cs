namespace Turbo.Plugins
{
    public enum HeroClass : uint
    {
        DemonHunter = 0,
        Barbarian = 1,
        Wizard = 2,
        WitchDoctor = 3,
        Monk = 4,
        Crusader = 5,
        Necromancer = 6,

        All = uint.MaxValue - 1,
        None = uint.MaxValue
    }
}