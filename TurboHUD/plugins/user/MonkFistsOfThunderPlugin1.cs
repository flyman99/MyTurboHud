﻿using Turbo.Plugins.glq;

namespace Turbo.Plugins.LightningMod
{
    public class MonkFistsOfThunderPlugin1 : AbstractSkillHandler, ISkillHandler
    {
        public MonkFistsOfThunderPlugin1()
            : base(CastType.SimpleSkill, CastPhase.AutoCast, CastPhase.UseWpStart, CastPhase.Move, CastPhase.Attack, CastPhase.AttackIdle)
        {
            Enabled = true;
        }
        public override void Load(IController hud)
        {
            base.Load(hud);
            AssignedSnoPower = Hud.Sno.SnoPowers.Monk_FistsOfThunder;
            CreateCastRule()
                .IfInTown().ThenNoCastElseContinue()
                .IfCastingIdentify().ThenNoCastElseContinue()
                .IfCastingPortal().ThenNoCastElseContinue()
                .IfCanCastSimple().ThenContinueElseNoCast()
                .IfTrue(ctx =>
                {
                    if (ctx.Skill.Player.GetSetItemCount(755275) >= 6 && ctx.Skill.Player.GetSetItemCount(563257) >= 2)
                    {
                        return true;
                    }
                    return false;
                }).ThenContinueElseNoCast()
                .IfTrue(ctx =>
                {
                    if (Hud.Game.Me.Animation.ToString().Contains("_rapidstrikes_"))//正在百烈拳
                    {
                        return true;
                    }
                    if (Hud.Game.Me.Animation.ToString().Contains("_debilitatingblows_"))//正在伏魔破
                    {
                        return true;
                    }
                    return false;
                }).ThenContinueElseNoCast()
                .IfTrue(ctx =>
                {
                    if (ctx.Skill.Player.Powers.BuffIsActive(Hud.Sno.SnoPowers.Monk_Passive_CombinationStrike.Sno))//融会贯通
                    {
                        if (PublicClassPlugin.GetBuffLeftTime(ctx.Hud, Hud.Sno.SnoPowers.Monk_Passive_CombinationStrike.Sno, 4) < 1)
                        {
                            return true;
                        }
                    }
                    return false;
                }).ThenCastElseContinue()
                ;
        }
    }
}