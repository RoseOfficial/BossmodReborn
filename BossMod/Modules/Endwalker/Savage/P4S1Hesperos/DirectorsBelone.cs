namespace BossMod.Endwalker.Savage.P4S1Hesperos;

// state related to director's belone (debuffs) mechanic
// note that forbidden targets are selected either from bloodrake tethers (first instance of mechanic) or from tower types (second instance of mechanic)
class DirectorsBelone(BossModule module) : BossComponent(module)
{
    private bool _assigned;
    private BitMask _debuffForbidden;
    private BitMask _debuffTargets;
    private BitMask _debuffImmune;

    private const float _debuffPassRange = 3; // not sure about this...
    private const float _movementTolerance = 0.8f; // Distance threshold for movement updates
    private const float _stackTolerance = 1.0f; // Distance threshold for stack formation

    public override void Update()
    {
        if (!_assigned)
        {
            var coils = Module.FindComponent<BeloneCoils>();
            if (coils == null)
            {
                // assign from bloodrake tethers
                _debuffForbidden = Raid.WithSlot(false, true, true).Tethered(TetherID.Bloodrake).Mask();
                _assigned = true;
            }
            else if (coils.ActiveSoakers != BeloneCoils.Soaker.Unknown)
            {
                // assign from coils (note that it happens with some delay)
                _debuffForbidden = Raid.WithSlot(false, true, true).WhereActor(coils.IsValidSoaker).Mask();
                _assigned = true;
            }
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_debuffForbidden.None())
            return;

        if (!_debuffForbidden[slot])
        {
            // we should be grabbing debuff
            if (_debuffTargets.None())
            {
                // debuffs not assigned yet => spread and prepare to grab
                var stacked = Raid.WithoutSlot(false, true, true).InRadiusExcluding(actor, _debuffPassRange).Any();
                hints.Add("Debuffs: spread and prepare to handle!", stacked);
            }
            else if (_debuffImmune[slot])
            {
                hints.Add("Debuffs: failed to handle");
            }
            else if (_debuffTargets[slot])
            {
                hints.Add("Debuffs: OK", false);
            }
            else
            {
                hints.Add("Debuffs: grab!");
            }
        }
        else
        {
            // we should be passing debuff
            if (_debuffTargets.None())
            {
                var badStack = Raid.WithSlot(false, true, true).Exclude(slot).IncludedInMask(_debuffForbidden).OutOfRadius(actor.Position, _debuffPassRange).Any();
                hints.Add("Debuffs: stack and prepare to pass!", badStack);
            }
            else if (_debuffTargets[slot])
            {
                hints.Add("Debuffs: pass!");
            }
            else
            {
                hints.Add("Debuffs: avoid", false);
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_debuffForbidden.None())
            return;

        if (!_debuffForbidden[slot])
        {
            // We should be grabbing debuff - position to intercept from forbidden players
            PositionForDebuffGrab(actor, hints);
        }
        else
        {
            // We should be passing debuff - position to pass to eligible players
            PositionForDebuffPass(actor, hints);
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        var forbidden = Raid.WithSlot(true, true, true).IncludedInMask(_debuffForbidden).FirstOrDefault().Item2;
        if (forbidden != null)
        {
            hints.Add($"Stack: {(forbidden.Role is Role.Tank or Role.Healer ? "Tanks/Healers" : "DD")}");
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (_debuffForbidden.None())
            return;

        // Draw players with different colors based on their role in the mechanic
        var failingPlayers = _debuffForbidden & _debuffTargets;
        foreach ((var i, var player) in Raid.WithSlot(false, true, true))
        {
            var color = Colors.PlayerGeneric;
            
            if (failingPlayers[i])
                color = Colors.Danger; // Failed to pass debuff
            else if (_debuffForbidden[i])
                color = Colors.Object; // Forbidden player (needs to pass)
            else if (_debuffTargets[i])
                color = Colors.Safe; // Successfully grabbed debuff
            else if (_debuffImmune[i])
                color = Colors.Vulnerable; // Immune/failed to grab
            
            Arena.Actor(player, color);
        }
        
        // Draw debuff transfer zones when debuffs are active
        if (_debuffTargets.Any())
        {
            foreach ((var i, var player) in Raid.WithSlot(false, true, true))
            {
                if (_debuffForbidden[i] && _debuffTargets[i])
                {
                    // Show pass range around forbidden players with debuffs
                    Arena.AddCircle(player.Position, _debuffPassRange, Colors.Object, 1);
                }
            }
        }
        else
        {
            // Pre-debuff phase: show clustering area for forbidden players
            var forbiddenPlayers = Raid.WithSlot(false, true, true).IncludedInMask(_debuffForbidden);
            if (forbiddenPlayers.Any())
            {
                var clusterCenter = new WPos(
                    forbiddenPlayers.Average(p => p.Item2.Position.X),
                    forbiddenPlayers.Average(p => p.Item2.Position.Z)
                );
                
                // Show clustering area
                Arena.AddCircle(clusterCenter, 2.0f, Colors.Object, 1);
                
                // Show intercept positions for eligible players
                var eligiblePlayers = Raid.WithSlot(false, true, true)
                    .WhereActor(p => !_debuffForbidden[Raid.FindSlot(p.InstanceID)])
                    .ToList();
                
                for (int j = 0; j < eligiblePlayers.Count; j++)
                {
                    var spreadRadius = _debuffPassRange + 1.5f;
                    var angleStep = 360f / eligiblePlayers.Count;
                    var angle = j * angleStep;
                    var radians = angle * MathF.PI / 180f;
                    var interceptPos = clusterCenter + new WDir(MathF.Sin(radians), MathF.Cos(radians)) * spreadRadius;
                    
                    // Show intercept position
                    Arena.AddCircle(interceptPos, 1.0f, Colors.Safe, 1);
                }
            }
        }
    }

    public override void OnStatusGain(Actor actor, ActorStatus status)
    {
        switch ((SID)status.ID)
        {
            case SID.RoleCall:
                _debuffTargets.Set(Raid.FindSlot(actor.InstanceID));
                break;
            case SID.Miscast:
                _debuffImmune.Set(Raid.FindSlot(actor.InstanceID));
                break;
        }
    }

    public override void OnStatusLose(Actor actor, ActorStatus status)
    {
        switch ((SID)status.ID)
        {
            case SID.RoleCall:
                _debuffTargets.Clear(Raid.FindSlot(actor.InstanceID));
                break;
            case SID.Miscast:
                _debuffImmune.Clear(Raid.FindSlot(actor.InstanceID));
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.CursedCasting1 or AID.CursedCasting2)
            _debuffForbidden.Reset();
    }

    private void ApplyMovementHint(Actor actor, WPos targetPos, AIHints hints, float tolerance = _movementTolerance)
    {
        var distanceToTarget = (targetPos - actor.Position).Length();
        if (distanceToTarget > tolerance)
        {
            // Smooth movement vector calculation
            var direction = (targetPos - actor.Position).Normalized();
            hints.ForcedMovement = direction.ToVec3(actor.PosRot.Y);
        }
    }

    private void PositionForDebuffGrab(Actor actor, AIHints hints)
    {
        // Find forbidden players who need to pass their debuffs
        var forbiddenPlayers = Raid.WithSlot(false, true, true).IncludedInMask(_debuffForbidden);
        var actorSlot = Raid.FindSlot(actor.InstanceID);
        
        if (_debuffTargets.None())
        {
            // Debuffs not assigned yet - position strategically around forbidden players
            if (forbiddenPlayers.Any())
            {
                // Calculate optimal spread positions around the forbidden stack
                var forbiddenCenter = new WPos(
                    forbiddenPlayers.Average(p => p.Item2.Position.X),
                    forbiddenPlayers.Average(p => p.Item2.Position.Z)
                );
                
                // Get all eligible players for coordinated positioning
                var eligiblePlayers = Raid.WithSlot(false, true, true)
                    .WhereActor(p => !_debuffForbidden[Raid.FindSlot(p.InstanceID)])
                    .ToList();
                
                // Find our position among eligible players
                var ourIndex = eligiblePlayers.FindIndex(p => p.Item2 == actor);
                var totalEligible = eligiblePlayers.Count;
                
                if (ourIndex >= 0 && totalEligible > 0)
                {
                    // Create a circle of positions around the forbidden stack
                    var spreadRadius = _debuffPassRange + 1.5f; // Just outside pass range initially
                    var angleStep = 360f / totalEligible;
                    var ourAngle = ourIndex * angleStep;
                    var radians = ourAngle * MathF.PI / 180f;
                    
                    var targetPos = forbiddenCenter + new WDir(MathF.Sin(radians), MathF.Cos(radians)) * spreadRadius;
                    
                    // Ensure position is within arena bounds
                    targetPos = Arena.ClampToBounds(targetPos);
                    
                    // Maintain spread distance from other eligible players
                    foreach (var (_, player) in eligiblePlayers)
                    {
                        if (player != actor)
                        {
                            hints.AddForbiddenZone(ShapeDistance.Circle(player.Position, _debuffPassRange));
                        }
                    }
                    
                    // Apply smooth movement to target position
                    ApplyMovementHint(actor, targetPos, hints, _stackTolerance);
                }
            }
            else
            {
                // No forbidden players yet - spread around center
                var slot = Raid.FindSlot(actor.InstanceID);
                var angle = slot * 45f; // 8 positions around the clock
                var radians = angle * MathF.PI / 180f;
                var targetPos = Arena.Center + new WDir(MathF.Sin(radians), MathF.Cos(radians)) * 8f;
                
                ApplyMovementHint(actor, targetPos, hints, 1.5f);
            }
        }
        else if (_debuffTargets.Any() && !_debuffTargets[actorSlot] && !_debuffImmune[actorSlot])
        {
            // Debuffs are assigned but we don't have one - actively intercept from forbidden players
            var availableForbiddenWithDebuff = forbiddenPlayers
                .Where(p => _debuffTargets[Raid.FindSlot(p.Item2.InstanceID)])
                .OrderBy(p => (p.Item2.Position - actor.Position).LengthSq())
                .ToList();
                
            if (availableForbiddenWithDebuff.Any())
            {
                // Move toward the closest forbidden player with a debuff
                var forbiddenPlayer = availableForbiddenWithDebuff.First().Item2;
                var directionToForbidden = (forbiddenPlayer.Position - actor.Position).Normalized();
                
                // Position to intercept - move closer to ensure we're in range
                var interceptDistance = Math.Max(0.5f, _debuffPassRange - 1.0f);
                var targetPos = forbiddenPlayer.Position - directionToForbidden * interceptDistance;
                
                // Ensure we're within arena bounds
                targetPos = Arena.ClampToBounds(targetPos);
                
                // Apply smooth movement to intercept position
                ApplyMovementHint(actor, targetPos, hints);
                
                // Add forbidden zones around other forbidden players to avoid interference
                foreach (var (_, forbiddenActor) in forbiddenPlayers)
                {
                    if (forbiddenActor != forbiddenPlayer)
                    {
                        hints.AddForbiddenZone(ShapeDistance.Circle(forbiddenActor.Position, _debuffPassRange - 0.5f));
                    }
                }
            }
        }
        else if (_debuffTargets[actorSlot])
        {
            // We successfully grabbed a debuff - move to safe position
            var safePos = Arena.Center;
            ApplyMovementHint(actor, safePos, hints, 2f);
        }
    }

    private void PositionForDebuffPass(Actor actor, AIHints hints)
    {
        // Find eligible players who can grab debuffs
        var eligiblePlayers = Raid.WithSlot(false, true, true).WhereActor(p => !_debuffForbidden[Raid.FindSlot(p.InstanceID)]);
        
        if (_debuffTargets.None())
        {
            // Debuffs not assigned yet - stack with other forbidden players and prepare to pass
            var otherForbiddenPlayers = Raid.WithSlot(false, true, true)
                .IncludedInMask(_debuffForbidden)
                .Where(p => p.Item2 != actor);
                
            if (otherForbiddenPlayers.Any())
            {
                // Improved clustering: Calculate optimal stack position considering eligible players
                var forbiddenPositions = otherForbiddenPlayers.Select(p => p.Item2.Position).ToList();
                forbiddenPositions.Add(actor.Position);
                
                // Calculate center of forbidden players
                var stackCenter = new WPos(
                    forbiddenPositions.Average(p => p.X),
                    forbiddenPositions.Average(p => p.Z)
                );
                
                // Adjust stack position to be more accessible to eligible players
                var eligibleCenter = new WPos(
                    eligiblePlayers.Average(p => p.Item2.Position.X),
                    eligiblePlayers.Average(p => p.Item2.Position.Z)
                );
                
                // Move stack position slightly toward eligible players for easier access
                var directionToEligible = (eligibleCenter - stackCenter).Normalized();
                var adjustedStackPos = stackCenter + directionToEligible * 1.0f;
                
                // Ensure stack position is within arena bounds
                var clampedStackPos = Arena.ClampToBounds(adjustedStackPos);
                
                // Apply smooth movement to stack position
                ApplyMovementHint(actor, clampedStackPos, hints, _stackTolerance);
            }
            else
            {
                // Single forbidden player - position optimally for eligible players to access
                var eligibleCenter = eligiblePlayers.Any() ? new WPos(
                    eligiblePlayers.Average(p => p.Item2.Position.X),
                    eligiblePlayers.Average(p => p.Item2.Position.Z)
                ) : Arena.Center;
                
                // Position between center and eligible players for easy access
                var optimalPos = WPos.Lerp(Arena.Center, eligibleCenter, 0.3f);
                ApplyMovementHint(actor, optimalPos, hints, 1.5f);
            }
        }
        else if (_debuffTargets[Raid.FindSlot(actor.InstanceID)])
        {
            // We have a debuff and need to pass it - move to eligible players
            var availableEligible = eligiblePlayers
                .Where(p => !_debuffTargets[Raid.FindSlot(p.Item2.InstanceID)] && !_debuffImmune[Raid.FindSlot(p.Item2.InstanceID)])
                .OrderBy(p => (p.Item2.Position - actor.Position).LengthSq())
                .ToList();
                
            if (availableEligible.Any())
            {
                // Move toward the closest available eligible player
                var eligiblePlayer = availableEligible.First().Item2;
                var directionToEligible = (eligiblePlayer.Position - actor.Position).Normalized();
                var targetPos = eligiblePlayer.Position - directionToEligible * (_debuffPassRange - 0.8f);
                
                // Apply smooth movement to pass position
                ApplyMovementHint(actor, targetPos, hints, _stackTolerance);
            }
        }
        else
        {
            // We don't have a debuff - position safely away from eligible players
            foreach (var (_, player) in eligiblePlayers)
            {
                hints.AddForbiddenZone(ShapeDistance.Circle(player.Position, _debuffPassRange + 1f));
            }
        }
    }
}
