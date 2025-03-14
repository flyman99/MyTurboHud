namespace Turbo.Plugins
{
    using System.Collections.Generic;

    public enum CastPhase { Collect, AutoCast, PreAttack, Attack, AttackIdle, Move, UsePortalStart, UseTpStart, UseWpStart }

    public interface ISkillHandler
    {
        HashSet<CastPhase> SupportedPhases { get; }
        ISnoPower AssignedSnoPower { get; }
        int? Rune { get; }
        void HandleCastPhase(IPlayerSkill skill, CastPhase phase);
        void CalculatePriority(IPlayerSkill skill, IMonster monster, ref double priority);
    }
}
