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
        // Death Sequence
        var death = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => _controller.IsDead()),
            new ActionNode(() => _controller.DeathAction())
        });

        // Return to Start Sequence
        var returnToStart = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => _controller.ShouldReturnToStart()),
            new ActionNode(() => _controller.ReturnToStartAction())
        });

        // Chase Sequence
        var chase = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => _controller.ShouldChase()),
            new ActionNode(() => _controller.ChaseAction())
        });

        // Idle Sequence (Default/Fallback)
        var idle = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => _controller.ShouldIdle()),
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
}