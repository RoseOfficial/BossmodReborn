namespace BossMod.Endwalker.Savage.P4S1Hesperos;

// state related to inversive chlamys mechanic (tethers)
// note that forbidden targets are selected either from bloodrake tethers (first instance of mechanic) or from tower types (second instance of mechanic)
class InversiveChlamys(BossModule module) : BossComponent(module)
{
    private bool _assigned;
    private BitMask _tetherForbidden;
    private BitMask _tetherTargets;
    private BitMask _tetherInAOE;

    private const float _aoeRange = 5;

    public bool TethersActive => _tetherTargets.Any();

    public override void Update()
    {
        if (!_assigned)
        {
            var coils = Module.FindComponent<BeloneCoils>();
            if (coils == null)
            {
                // assign from bloodrake tethers
                _tetherForbidden = Raid.WithSlot(false, true, true).Tethered(TetherID.Bloodrake).Mask();
                _assigned = true;
            }
            else if (coils.ActiveSoakers != BeloneCoils.Soaker.Unknown)
            {
                // assign from coils (note that it happens with some delay)
                _tetherForbidden = Raid.WithSlot(false, true, true).WhereActor(coils.IsValidSoaker).Mask();
                _assigned = true;
            }
        }

        _tetherTargets = _tetherInAOE = default;
        if (_tetherForbidden.None())
            return;

        foreach ((var i, var player) in Raid.WithSlot(false, true, true).Tethered(TetherID.Chlamys))
        {
            _tetherTargets.Set(i);
            _tetherInAOE |= Raid.WithSlot(false, true, true).InRadiusExcluding(player, _aoeRange).Mask();
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_tetherForbidden.None())
            return;

        if (!_tetherForbidden[slot])
        {
            // we should be grabbing tethers
            if (_tetherTargets.None())
            {
                hints.Add("Tethers: prepare to intercept", false);
            }
            else if (!_tetherTargets[slot])
            {
                hints.Add("Tethers: intercept!");
            }
            else if (Raid.WithoutSlot(false, true, true).InRadiusExcluding(actor, _aoeRange).Any())
            {
                hints.Add("Tethers: GTFO from others!");
            }
            else
            {
                hints.Add("Tethers: OK", false);
            }
        }
        else
        {
            // we should be passing tethers
            if (_tetherTargets.None())
            {
                hints.Add("Tethers: prepare to pass", false);
            }
            else if (_tetherTargets[slot])
            {
                hints.Add("Tethers: pass!");
            }
            else if (_tetherInAOE[slot])
            {
                hints.Add("Tethers: GTFO from aoe!");
            }
            else
            {
                hints.Add("Tethers: avoid", false);
            }
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        var forbidden = Raid.WithSlot(true, true, true).IncludedInMask(_tetherForbidden).FirstOrDefault().Item2;
        if (forbidden != null)
        {
            hints.Add($"Intercept: {(forbidden.Role is Role.Tank or Role.Healer ? "DD" : "Tanks/Healers")}");
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_tetherForbidden.None())
            return;

        if (!_tetherForbidden[slot])
        {
            // We should be intercepting tethers - position to intercept from forbidden players
            PositionForTetherIntercept(actor, hints);
        }
        else
        {
            // We should be passing tethers - position to pass to eligible players and avoid AOE
            PositionForTetherPass(actor, hints);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (_tetherTargets.None())
            return;

        var failingPlayers = _tetherForbidden & _tetherTargets;
        foreach ((var i, var player) in Raid.WithSlot(false, true, true))
        {
            var failing = failingPlayers[i];
            var inAOE = _tetherInAOE[i];
            Arena.Actor(player, failing ? Colors.Danger : (inAOE ? Colors.PlayerInteresting : Colors.PlayerGeneric));

            if (player.Tether.ID == (uint)TetherID.Chlamys)
            {
                Arena.AddLine(player.Position, Module.PrimaryActor.Position, failing ? Colors.Danger : Colors.Safe);
                Arena.AddCircle(player.Position, _aoeRange, Colors.Danger);
            }
        }
    }

    private void PositionForTetherIntercept(Actor actor, AIHints hints)
    {
        // Find forbidden players who need to pass their tethers
        var forbiddenPlayers = Raid.WithSlot(false, true, true).IncludedInMask(_tetherForbidden);
        
        if (_tetherTargets.None())
        {
            // Tethers not assigned yet - position to be ready to intercept
            var centerPos = Arena.Center;
            var slot = Raid.FindSlot(actor.InstanceID);
            var angle = slot * 45f; // 8 positions around the clock
            var radians = angle * MathF.PI / 180f;
            var targetPos = centerPos + new WDir(MathF.Sin(radians), MathF.Cos(radians)) * 8f;
            
            // Maintain distance from other interceptors
            foreach (var (_, player) in Raid.WithSlot(false, true, true))
            {
                if (player != actor && !_tetherForbidden[Raid.FindSlot(player.InstanceID)])
                {
                    hints.AddForbiddenZone(ShapeDistance.Circle(player.Position, 4f));
                }
            }
            
            // Only apply forced movement if we're not already close to target position
            var distanceToTarget = (targetPos - actor.Position).Length();
            if (distanceToTarget > 1.5f)
            {
                hints.ForcedMovement = (targetPos - actor.Position).ToVec3(actor.PosRot.Y);
            }
        }
        else if (_tetherTargets.Any() && !_tetherTargets[Raid.FindSlot(actor.InstanceID)])
        {
            // Tethers are assigned but we don't have one - move to intercept from forbidden players
            var closestForbiddenWithTether = forbiddenPlayers
                .Where(p => _tetherTargets[Raid.FindSlot(p.Item2.InstanceID)])
                .OrderBy(p => (p.Item2.Position - actor.Position).LengthSq())
                .FirstOrDefault();
                
            if (closestForbiddenWithTether.Item2 != null)
            {
                // Position between forbidden player and boss to intercept tether
                var forbiddenPlayer = closestForbiddenWithTether.Item2;
                var bossPos = Module.PrimaryActor.Position;
                var directionToBoss = (bossPos - forbiddenPlayer.Position).Normalized();
                var targetPos = forbiddenPlayer.Position + directionToBoss * 2f;
                
                // Only apply forced movement if we're not already close to target position
                var distanceToTarget = (targetPos - actor.Position).Length();
                if (distanceToTarget > 1.5f)
                {
                    hints.ForcedMovement = (targetPos - actor.Position).ToVec3(actor.PosRot.Y);
                }
            }
        }
        else if (_tetherTargets[Raid.FindSlot(actor.InstanceID)])
        {
            // We have a tether - position away from others to avoid AOE overlap
            foreach (var (_, player) in Raid.WithSlot(false, true, true))
            {
                if (player != actor)
                {
                    hints.AddForbiddenZone(ShapeDistance.Circle(player.Position, _aoeRange + 1f));
                }
            }
        }
    }

    private void PositionForTetherPass(Actor actor, AIHints hints)
    {
        // Find eligible players who can intercept tethers
        var eligiblePlayers = Raid.WithSlot(false, true, true).WhereActor(p => !_tetherForbidden[Raid.FindSlot(p.InstanceID)]);
        
        if (_tetherTargets.None())
        {
            // Tethers not assigned yet - position with other forbidden players
            var otherForbiddenPlayers = Raid.WithSlot(false, true, true)
                .IncludedInMask(_tetherForbidden)
                .Where(p => p.Item2 != actor);
                
            if (otherForbiddenPlayers.Any())
            {
                // Stack with other forbidden players
                var stackPos = new WPos(
                    otherForbiddenPlayers.Average(p => p.Item2.Position.X),
                    otherForbiddenPlayers.Average(p => p.Item2.Position.Z)
                );
                
                // Only apply forced movement if we're not already close to stack position
                var distanceToStack = (stackPos - actor.Position).Length();
                if (distanceToStack > 1.5f)
                {
                    hints.ForcedMovement = (stackPos - actor.Position).ToVec3(actor.PosRot.Y);
                }
            }
            else
            {
                // Position centrally for easy access by eligible players
                var distanceToCenter = (Arena.Center - actor.Position).Length();
                if (distanceToCenter > 2f)
                {
                    hints.ForcedMovement = (Arena.Center - actor.Position).ToVec3(actor.PosRot.Y);
                }
            }
        }
        else if (_tetherTargets[Raid.FindSlot(actor.InstanceID)])
        {
            // We have a tether and need to pass it - move towards eligible players
            var closestEligible = eligiblePlayers
                .Where(p => !_tetherTargets[Raid.FindSlot(p.Item2.InstanceID)])
                .OrderBy(p => (p.Item2.Position - actor.Position).LengthSq())
                .FirstOrDefault();
                
            if (closestEligible.Item2 != null)
            {
                // Move towards the eligible player to pass the tether
                var eligiblePlayer = closestEligible.Item2;
                var directionToEligible = (eligiblePlayer.Position - actor.Position).Normalized();
                var targetPos = eligiblePlayer.Position - directionToEligible * 2f;
                
                // Only apply forced movement if we're not already close to target position
                var distanceToTarget = (targetPos - actor.Position).Length();
                if (distanceToTarget > 1.5f)
                {
                    hints.ForcedMovement = (targetPos - actor.Position).ToVec3(actor.PosRot.Y);
                }
            }
        }
        else
        {
            // We don't have a tether - stay away from AOE zones
            foreach (var (_, player) in Raid.WithSlot(false, true, true))
            {
                if (_tetherTargets[Raid.FindSlot(player.InstanceID)])
                {
                    hints.AddForbiddenZone(ShapeDistance.Circle(player.Position, _aoeRange + 1f));
                }
            }
        }
    }
}
