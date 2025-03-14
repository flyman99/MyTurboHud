using System.Collections.Generic;

namespace Turbo.Plugins
{
    public interface IActorQueryController
    {
        IHeadStone NearestHeadStone { get; }
        IMonster NearestBoss { get; }
        IMonster NearestKeywarden { get; }
        IMonster NearestElite { get; }
        IMonster NearestEliteButNoMinion { get; }
        bool IsEliteOrBossCloserThan(int range, bool includeMinion = true);
        IMonster NearestGoblin { get; }
        IShrine NearestShrine { get; }

        IClickableActor NearestNormalChest { get; }
        IClickableActor NearestResplendentChest { get; }
        IClickableActor NearestDoor { get; }
        IClickableActor NearestCursedEvent { get; }
        IActor Stash { get; }
        IActor KanaiCube { get; }
        IActor Waypoint { get; }
        IActor BookOfCain { get; }

        List<IActor> HealthGlobes { get; }
        IActor NearestHealthGlobe { get; }

        List<IActor> PowerGlobes { get; }
        List<IActor> RiftOrbs { get; }

        int OnScreenMonsterCount { get; }

        IEnumerable<ISkillEffect> GetSkillEffectActors(SkillEffectType effectType);
    }
}
