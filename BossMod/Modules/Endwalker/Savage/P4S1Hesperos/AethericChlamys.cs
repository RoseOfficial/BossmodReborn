namespace BossMod.Endwalker.Savage.P4S1Hesperos;

// AI component for automatic responses to Aetheric Chlamys (tether preparation)
class AethericChlamys(BossModule module) : BossComponent(module)
{
    private bool _castStarted;
    private DateTime _castEnd;
    private BitMask _forbiddenPlayers;
    private BitMask _interceptors;
    private bool _assigned;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.AethericChlamys)
        {
            _castStarted = true;
            _castEnd = Module.CastFinishAt(spell);
            _assigned = false;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.AethericChlamys)
        {
            _castStarted = false;
        }
    }

    public override void Update()
    {
        if (!_castStarted || _assigned)
            return;

        // Determine who needs to intercept tethers based on current Bloodrake tethers
        var bloodrakeTethered = Raid.WithSlot(false, true, true).Tethered(TetherID.Bloodrake);
        if (bloodrakeTethered.Any())
        {
            _forbiddenPlayers = bloodrakeTethered.Mask();
            _interceptors = Raid.WithSlot(false, true, true).WhereActor(p => !_forbiddenPlayers[Raid.FindSlot(p.InstanceID)]).Mask();
            _assigned = true;
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!_castStarted || !_assigned)
            return;

        var timeToEnd = (float)(_castEnd - WorldState.CurrentTime).TotalSeconds;
        
        if (_interceptors[slot])
        {
            // This player needs to intercept tethers - position for tether intercept
            PositionForTetherIntercept(actor, hints, timeToEnd);
        }
        else if (_forbiddenPlayers[slot])
        {
            // This player should avoid tethers - position away from intercept zone
            PositionForTetherAvoidance(actor, hints, timeToEnd);
        }
    }

    private void PositionForTetherIntercept(Actor actor, AIHints hints, float timeToEnd)
    {
        // Position in middle of arena to intercept tethers
        var centerPos = Arena.Center;
        var distanceFromCenter = (actor.Position - centerPos).Length();
        
        // Move closer to center if too far away
        if (distanceFromCenter > 5f)
        {
            var directionToCenter = (centerPos - actor.Position).Normalized();
            var targetPos = centerPos + directionToCenter * 3f;
            hints.AddForbiddenZone(ShapeDistance.InvertedCircle(targetPos, 2f));
        }
        
        // Only apply forced movement if we're not already close to center
        if (distanceFromCenter > 2f)
        {
            hints.ForcedMovement = (centerPos - actor.Position).ToVec3(actor.PosRot.Y);
        }
    }

    private void PositionForTetherAvoidance(Actor actor, AIHints hints, float timeToEnd)
    {
        // Position away from center to avoid accidentally intercepting tethers
        var centerPos = Arena.Center;
        var distanceFromCenter = (actor.Position - centerPos).Length();
        
        // Move away from center if too close
        if (distanceFromCenter < 8f)
        {
            var directionFromCenter = (actor.Position - centerPos).Normalized();
            var targetPos = centerPos + directionFromCenter * 10f;
            hints.AddForbiddenZone(ShapeDistance.Circle(centerPos, 7f));
        }
        
        // Determine safe positioning based on role and upcoming mechanics
        var coils = Module.FindComponent<BeloneCoils>();
        if (coils?.ActiveSoakers == BeloneCoils.Soaker.TankOrHealer && actor.Role is Role.Tank or Role.Healer)
        {
            // Position for tower soaking
            PositionForTowerSoak(actor, hints);
        }
        else if (coils?.ActiveSoakers == BeloneCoils.Soaker.DamageDealer && actor.Role is Role.Melee or Role.Ranged)
        {
            // Position for tower soaking
            PositionForTowerSoak(actor, hints);
        }
        else
        {
            // Position at safe distance from towers and center
            var safePos = GetSafeClockPosition(actor);
            var distanceToSafe = (safePos - actor.Position).Length();
            if (distanceToSafe > 1.5f)
            {
                hints.ForcedMovement = (safePos - actor.Position).ToVec3(actor.PosRot.Y);
            }
        }
    }

    private void PositionForTowerSoak(Actor actor, AIHints hints)
    {
        // Find assigned tower position based on role
        var towerPos = GetTowerPosition(actor);
        var distanceToTower = (towerPos - actor.Position).Length();
        if (distanceToTower > 1.5f)
        {
            hints.ForcedMovement = (towerPos - actor.Position).ToVec3(actor.PosRot.Y);
        }
        hints.AddForbiddenZone(ShapeDistance.InvertedCircle(towerPos, 3f));
    }

    private WPos GetTowerPosition(Actor actor)
    {
        // Basic tower positioning - this would need to be refined based on actual tower mechanics
        var center = Arena.Center;
        var radius = 12f;
        
        // Assign positions based on role and slot
        var angle = actor.Role switch
        {
            Role.Tank => 0f,        // North
            Role.Healer => 180f,    // South
            Role.Melee => 90f,      // East
            Role.Ranged => 270f,    // West
            _ => 0f
        };
        
        var radians = angle * MathF.PI / 180f;
        return center + new WDir(MathF.Sin(radians), MathF.Cos(radians)) * radius;
    }

    private WPos GetSafeClockPosition(Actor actor)
    {
        // Position at clock spots for safe tether intercept
        var center = Arena.Center;
        var radius = 15f;
        
        // Assign clock positions based on party slot
        var slot = Raid.FindSlot(actor.InstanceID);
        var angle = slot * 45f; // 8 positions around the clock
        
        var radians = angle * MathF.PI / 180f;
        return center + new WDir(MathF.Sin(radians), MathF.Cos(radians)) * radius;
    }
}