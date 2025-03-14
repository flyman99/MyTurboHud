namespace Turbo.Plugins
{
    public sealed class AvoidableDefinition
    {
        public AvoidableType Type { get; set; }
        public ISnoActor SnoActor { get; set; }
        public float Radius { get; set; }
        public bool IsDeadMonster { get; set; }

        public AvoidableWeight Weight { get; set; }
        public bool InstantDeath { get; set; }
    }
}
