using System.Collections.Generic;
using BehaviorTree;
using UnityEngine;

public class MiniBossBT : MonoBehaviour
{
    private BTNode _root;
    private MiniBossController _controller;

    public MiniBossBT(MiniBossController controller)
    {
        _controller = controller;
        BuildTree();
    }

    public void BuildTree()
    {
        // Retreat Sequence - when health is low or no allies left
        var retreat = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => _controller.ShouldRetreat()),
            new ActionNode(() => _controller.MoveToBaseSpawn()),
            new ActionNode(() => _controller.HealAtSpawn()),
            new ActionNode(() => _controller.SummonUnitsUntilLimit())
        });

        // Frenzy Sequence - late game or desperate situation
        var frenzy = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => _controller.ShouldFrenzy()),
            new ActionNode(() => _controller.ReduceCooldowns()),
            new ActionNode(() => _controller.ForceAdvanceToNextArea())
        });

        // Engage Sequence - when enemies are in range, select best action
        var engageSelector = new SelectorNode(new BTNode[]
        {
            new SequenceNode(new BTNode[]
            {
                new ConditionNode(() => _controller.CanCastDarkBolt()),
                new ActionNode(() => _controller.CastDarkBoltAtTarget())
            }),
            new SequenceNode(new BTNode[]
            {
                new ConditionNode(() => _controller.CanSummonSkeletons()),
                new ActionNode(() => _controller.SummonSkeletonsUntilLimit())
            }),
            new SequenceNode(new BTNode[]
            {
                new ConditionNode(() => _controller.CanBuffAllies()),
                new ActionNode(() => _controller.BuffAlliesDamageBoost())
            })
        });
        var engage = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => _controller.EnemyInRange()),
            engageSelector
        });

        // Attack Sequence - coordinated army advancement
        var attack = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => _controller.ReadyToAdvance()),
            new ActionNode(() => _controller.CommandArmyMoveToNextArea()),
            new ActionNode(() => _controller.SetFlagEnemy())
        });

        // Command Sequence - maintain army and support allies
        var command = new SequenceNode(new BTNode[]
        {
            new ConditionNode(() => _controller.ArmySizeBelowLimit()),
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
}