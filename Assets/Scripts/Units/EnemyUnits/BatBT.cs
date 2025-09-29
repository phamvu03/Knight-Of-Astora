using UnityEngine;
using BehaviorTree;

public class BatBT
{
    private BTNode _root;
    private BatBlackboard _bb;
    private BatController _controller;

    public BatBT(BatBlackboard blackboard, BatController controller)
    {
        _bb = blackboard;
        _controller = controller;
        BuildTree();
    }

    public void BuildTree()
    {
        // Death Sequence (Highest Priority)
        var death = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => DeathCondition()),
            new ActionNode(() => _controller.DeathAction())
        });

        // Return to Start Sequence
        var returnToStart = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => ReturnCondition()),
            new ActionNode(() => _controller.ReturnToStartAction())
        });

        // Chase Sequence
        var chase = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => ChaseCondition()),
            new ActionNode(() => _controller.ChaseAction())
        });

        // Idle Sequence (Default/Fallback)
        var idle = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => IdleCondition()),
            new ActionNode(() => _controller.IdleAction())
        });

        // Main Selector - Priority order matters
        _root = new SelectorNode(new BTNode[]
        {
            death,          // Highest priority
            returnToStart,  // Second priority
            chase,          // Third priority
            idle            // Lowest priority (fallback)
        });
    }

    public void Tick()
    {
        if (_root != null)
        {
            _root.Evaluate();
        }
    }

    // --- BT Condition Methods ---

    private bool DeathCondition()
    {
        // Use inherited hp field instead of currentHP
        return _bb.isDead || _bb.hp <= 0;
    }

    private bool ReturnCondition()
    {
        if (_bb.isDead) return false;
        
        float distanceFromStart = _bb.GetDistanceToStart(_controller.transform.position);
        float distanceToTarget = _bb.GetDistanceToTarget();
        
        // Return if too far from start OR too far from target while chasing
        return (distanceFromStart > _bb.maxDistanceFromStart) || 
               (distanceToTarget > _bb.maxChasingDistance && _bb.isChasing) ||
               (_bb.IsHealthBelowPercent(0.2f) && !_bb.isAtSpawn); // Return when low health
    }

    private bool ChaseCondition()
    {
        if (_bb.isDead || _bb.isReturning) return false;
        
        // Chase if player detected and within limits
        if (_bb.HasTarget())
        {
            float distanceFromStart = _bb.GetDistanceToStart(_controller.transform.position);
            float distanceToTarget = _bb.GetDistanceToTarget();
            
            return distanceToTarget <= _bb.detectionRange && 
                   distanceFromStart <= _bb.maxDistanceFromStart &&
                   distanceToTarget <= _bb.maxChasingDistance &&
                   !_bb.IsHealthBelowPercent(0.1f); // Don't chase when critically low health
        }
        
        return false;
    }

    private bool IdleCondition()
    {
        // Idle is the default state when no other conditions are met
        return !_bb.isDead && !_bb.isChasing && !_bb.isReturning && !_bb.isEngaging;
    }
}