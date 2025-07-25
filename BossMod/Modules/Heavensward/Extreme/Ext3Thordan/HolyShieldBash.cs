﻿namespace BossMod.Heavensward.Extreme.Ex3Thordan;

sealed class HolyShieldBash(BossModule module) : Components.GenericWildCharge(module, 3f)
{
    public override PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor)
        => PlayerRoles[playerSlot] == PlayerRole.Target ? PlayerPriority.Interesting : PlayerPriority.Irrelevant;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.HolyShieldBash:
                foreach (var (i, p) in Raid.WithSlot(true, true, true))
                {
                    // TODO: we don't really account for possible MT changes...
                    PlayerRoles[i] = p.InstanceID == spell.TargetID ? PlayerRole.Target : p.Role != Role.Tank ? PlayerRole.ShareNotFirst : p.InstanceID != Module.PrimaryActor.TargetID ? PlayerRole.Share : PlayerRole.Avoid;
                }
                break;
            case (uint)AID.SpearOfTheFury:
                Source = caster;
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.HolyShieldBash:
                if (NumCasts == 0)
                    NumCasts = 1;
                break;
            case (uint)AID.SpearOfTheFury:
                Source = null;
                NumCasts = 2;
                break;
        }
    }
}
