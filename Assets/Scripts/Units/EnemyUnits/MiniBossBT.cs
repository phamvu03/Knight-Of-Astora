using System.Collections.Generic;
using BehaviorTree;
using UnityEngine;

public class MiniBossBT : MonoBehaviour
{
    private BTNode _root;
    private EnemyBlackboard _bb;
    private MiniBossController _controller;

    public MiniBossBT(EnemyBlackboard bb, MiniBossController controller)
    {
        _bb = bb;
        _controller = controller;
        BuildTree();
    }
    
    public void BuildTree()
    {
        // Retreat Sequence - when health is low or no allies left
        var retreat = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => RetreatCondition()),
            new ActionNode(() => _controller.MoveToBaseSpawn()),
            new ActionNode(() => _controller.HealAtSpawn()),
            new ActionNode(() => _controller.SummonUnitsUntilLimit(_bb.maxArmySize))
        });

        // Frenzy Sequence - late game or desperate situation
        var frenzy = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => FrenzyCondition()),
            new ActionNode(() => _controller.ReduceCooldowns()),
            new ActionNode(() => _controller.ForceAdvanceToNextArea())
        });

        // Engage Sequence - when enemies are in range, select best action
        var engageSelector = new SelectorNode(new BTNode[]
        {
            new SequenceNode(new BTNode[]
            {
                new ConditionNode(() => CanCastDarkBolt()),
                new ActionNode(() => _controller.CastDarkBoltAtTarget())
            }),
            new SequenceNode(new BTNode[]
            {
                new ConditionNode(() => CanSummonSkeletons()),
                new ActionNode(() => _controller.SummonSkeletonsUntilLimit(_bb.maxArmySize))
            }),
            new SequenceNode(new BTNode[]
            {
                new ConditionNode(() => CanBuffAllies()),
                new ActionNode(() => _controller.BuffAlliesDamageBoost())
            })
        });
        var engage = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => EnemyInRange()),
            engageSelector
        });

        // Attack Sequence - coordinated army advancement
        var attack = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => ReadyToAdvance()),
            new ActionNode(() => _controller.CommandArmyMoveToNextArea()),
            new ActionNode(() => _controller.SetFlagEnemy())
        });

        // Command Sequence - maintain army and support allies
        var command = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => ArmySizeBelowLimit(_bb.maxArmySize)),
            new ActionNode(() => _controller.SummonSkeletons()),
            new ActionNode(() => _controller.BuffAlliesHealNearby())
        });

        // Main Selector - priority order matters
        _root = new SelectorNode(new BTNode[]
        {
            retreat,
            frenzy,
            engage,
            attack,
            command
        });
    }

    public void Tick()
    {
        _root?.Evaluate();
    }

    // --- BT Condition Methods ---
    private bool RetreatCondition()
    {
        // HP < 30% OR no nearby allies OR critically damaged
        return _bb.IsHealthBelowPercent(0.3f) || 
               _bb.nearbyAllies.Count == 0 || 
               (_bb.hp < _bb.hpLowThreshold && _bb.currentArmySize < 3);
    }
    
    private bool FrenzyCondition()
    {
        // Game time > 10min OR army size critically low OR HP very low
        return _bb.GetGameTime() > 600f || 
               _bb.currentArmySize < 3 || 
               _bb.IsHealthBelowPercent(0.15f);
    }
    
    private bool EnemyInRange()
    {
        // Check if any enemy is within detection range
        return _bb.detectedEnemies.Count > 0 && 
               _bb.targetPlayer != null && 
               Vector3.Distance(transform.position, _bb.targetPlayer.position) <= _bb.detectionRange;
    }
    
    private bool ReadyToAdvance()
    {
        // Army size is good AND not recently advanced AND has targets ahead
        return _bb.currentArmySize >= (_bb.maxArmySize * 0.6f) && 
               !_bb.hasCommandedArmy && 
               _bb.detectedEnemies.Count > 0 &&
               Time.time > _bb.lastAreaAdvanceTime + 30f; // Cooldown between advances
    }
    
    private bool ArmySizeBelowLimit(int limit)
    {
        // Current army size is below the specified limit
        return _bb.currentArmySize < limit;
    }

    private bool CanCastDarkBolt()
    {
        // Can cast if cooldown is ready and has valid target
        return _bb.CanCastDarkBolt() && 
               _bb.targetPlayer != null && 
               Vector3.Distance(transform.position, _bb.targetPlayer.position) <= _bb.darkBoltRange;
    }

    private bool CanSummonSkeletons()
    {
        // Can summon if cooldown is ready and army is below max
        return _bb.CanSummon() && _bb.currentArmySize < _bb.maxArmySize;
    }

    private bool CanBuffAllies()
    {
        // Can buff if cooldown is ready and has nearby allies
        return _bb.CanBuffAllies() && _bb.nearbyAllies.Count > 0;
    }
}