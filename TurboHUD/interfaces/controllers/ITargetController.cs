namespace Turbo.Plugins
{
    using System;

    public interface ITargetController
    {
        float MaxAttackRange { get; set; } // default -1
        bool AllowSkipMonsters { get; set; } // default true

        int TemporarySkipCountAdjustment { get; set; } // default 0
        Func<IController, int> ExtraSkipCount { get; set; } // default 0
        Func<IController, bool> AllowSkipElites { get; set; } // default false

        double CurrentAttackRange { get; }
        bool CanSkipMonsters { get; }
        int MonsterCountToSkip { get; }

        IMonster TargetOverride { get; set; }
        IWatch TargetOverrideLastChanged { get; }

        void NoSkipFor(int milliseconds);

        // ---------

        IMonster NearestTarget { get; }
        int TargetCount { get; }

        IMonster CurrentTarget { get; set; }
        IMonster CurrentTargetBeforeSkip { get; }
        IWatch CurrentTargetLastDamaged { get; }

        void PaintTopDebug(ClipState clipState);
    }
}
