namespace BossMod.Endwalker.Savage.P4S1Hesperos;

// AI component for automatic responses to Decollation (raidwide AOE)
class Decollation(BossModule module) : BossComponent(module)
{
    private bool _castStarted;
    private DateTime _castEnd;
    private bool _mitigationUsed;
    private bool _healingUsed;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.Decollation)
        {
            _castStarted = true;
            _castEnd = Module.CastFinishAt(spell);
            _mitigationUsed = false;
            _healingUsed = false;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.Decollation)
        {
            _castStarted = false;
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!_castStarted)
            return;

        var timeToImpact = (float)(_castEnd - WorldState.CurrentTime).TotalSeconds;
        
        // Apply mitigation during cast (2-4 seconds before impact)
        if (timeToImpact is > 1f and < 4f && !_mitigationUsed)
        {
            ApplyMitigation(actor, hints, timeToImpact);
        }

        // Apply healing after impact (0-2 seconds after cast finishes)
        if (timeToImpact <= 1f && !_healingUsed)
        {
            ApplyHealing(actor, hints);
        }

        // Positioning: stack for optimal healing range
        if (timeToImpact > 0)
        {
            hints.AddForbiddenZone(ShapeDistance.InvertedCircle(Arena.Center, 15f));
        }
    }

    private void ApplyMitigation(Actor actor, AIHints hints, float timeToImpact)
    {
        switch (actor.Role)
        {
            case Role.Tank:
                // Tank role mitigation
                if (actor.Class == Class.WAR)
                    hints.ActionsToExecute.Push(ActionID.MakeSpell(WAR.AID.Reprisal), Module.PrimaryActor, ActionQueue.Priority.High, timeToImpact - 1f);
                else if (actor.Class == Class.PLD)
                    hints.ActionsToExecute.Push(ActionID.MakeSpell(PLD.AID.Reprisal), Module.PrimaryActor, ActionQueue.Priority.High, timeToImpact - 1f);
                else if (actor.Class == Class.DRK)
                    hints.ActionsToExecute.Push(ActionID.MakeSpell(DRK.AID.Reprisal), Module.PrimaryActor, ActionQueue.Priority.High, timeToImpact - 1f);
                else if (actor.Class == Class.GNB)
                    hints.ActionsToExecute.Push(ActionID.MakeSpell(GNB.AID.Reprisal), Module.PrimaryActor, ActionQueue.Priority.High, timeToImpact - 1f);
                break;

            case Role.Melee:
                // Melee role mitigation
                hints.ActionsToExecute.Push(ActionID.MakeSpell(ClassShared.AID.Feint), Module.PrimaryActor, ActionQueue.Priority.High, timeToImpact - 1f);
                break;

            case Role.Ranged:
                // Ranged role mitigation
                if (actor.Class.GetClassCategory() == ClassCategory.Caster)
                    hints.ActionsToExecute.Push(ActionID.MakeSpell(ClassShared.AID.Addle), Module.PrimaryActor, ActionQueue.Priority.High, timeToImpact - 1f);
                else
                    hints.ActionsToExecute.Push(ActionID.MakeSpell(BRD.AID.Troubadour), actor, ActionQueue.Priority.High, timeToImpact - 1f);
                break;

            case Role.Healer:
                // Healer shields/mitigation
                if (actor.Class == Class.SCH)
                    hints.ActionsToExecute.Push(ActionID.MakeSpell(SCH.AID.Succor), actor, ActionQueue.Priority.High, timeToImpact - 1f);
                else if (actor.Class == Class.AST)
                    hints.ActionsToExecute.Push(ActionID.MakeSpell(AST.AID.AspectedHelios), actor, ActionQueue.Priority.High, timeToImpact - 1f);
                else if (actor.Class == Class.SGE)
                    hints.ActionsToExecute.Push(ActionID.MakeSpell(SGE.AID.EukrasianPrognosis), actor, ActionQueue.Priority.High, timeToImpact - 1f);
                break;
        }
        _mitigationUsed = true;
    }

    private void ApplyHealing(Actor actor, AIHints hints)
    {
        if (actor.Role != Role.Healer)
            return;

        // Post-damage healing
        switch (actor.Class)
        {
            case Class.WHM:
                hints.ActionsToExecute.Push(ActionID.MakeSpell(WHM.AID.MedicaII), actor, ActionQueue.Priority.High);
                break;
            case Class.SCH:
                hints.ActionsToExecute.Push(ActionID.MakeSpell(SCH.AID.Succor), actor, ActionQueue.Priority.High);
                break;
            case Class.AST:
                hints.ActionsToExecute.Push(ActionID.MakeSpell(AST.AID.Helios), actor, ActionQueue.Priority.High);
                break;
            case Class.SGE:
                hints.ActionsToExecute.Push(ActionID.MakeSpell(SGE.AID.Pneuma), actor, ActionQueue.Priority.High);
                break;
        }
        _healingUsed = true;
    }
}