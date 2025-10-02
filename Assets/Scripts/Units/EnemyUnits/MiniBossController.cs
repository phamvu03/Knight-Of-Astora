using UnityEngine;
using BehaviorTree;

public class MiniBossController : MonoBehaviour
{
    [Header("MiniBoss Settings")]
    public MiniBossBlackboard blackboard;

    private MiniBossBT _bt;

    private void Awake()
    {
        blackboard = new MiniBossBlackboard();
        blackboard.Initialize(transform);
    }

    private void Start()
    {
        _bt = new MiniBossBT(this);
    }

    private void Update()
    {
        _bt.Tick();
    }

    // Condition Methods
    public bool ShouldRetreat()
    {
        return blackboard.IsHealthBelowPercent(0.3f);
    }

    public bool ShouldFrenzy()
    {
        return blackboard.IsHealthBelowPercent(0.15f);
    }

    public bool EnemyInRange()
    {
        return blackboard.HasDetectedEnemies();
    }

    public bool ReadyToAdvance()
    {
        return blackboard.CanAdvance();
    }

    public bool ArmySizeBelowLimit()
    {
        return blackboard.ArmySizeBelowLimit();
    }

    public bool CanCastDarkBolt()
    {
        return blackboard.CanCastDarkBolt();
    }

    public bool CanSummonSkeletons()
    {
        return blackboard.CanSummonSkeletons();
    }

    public bool CanBuffAllies()
    {
        return blackboard.CanBuffAllies();
    }

    // Action Methods
    public BehaviorState MoveToBaseSpawn()
    {
        return BehaviorState.Success;
    }

    public BehaviorState HealAtSpawn()
    {
        return BehaviorState.Success;
    }

    public BehaviorState SummonUnitsUntilLimit()
    {
        return BehaviorState.Success;
    }

    public BehaviorState ReduceCooldowns()
    {
        return BehaviorState.Success;
    }

    public BehaviorState ForceAdvanceToNextArea()
    {
        return BehaviorState.Success;
    }

    public BehaviorState CastDarkBoltAtTarget()
    {
        return BehaviorState.Success;
    }

    public BehaviorState SummonSkeletonsUntilLimit()
    {
        return BehaviorState.Success;
    }

    public BehaviorState BuffAlliesDamageBoost()
    {
        return BehaviorState.Success;
    }

    public BehaviorState CommandArmyMoveToNextArea()
    {
        return BehaviorState.Success;
    }

    public BehaviorState SetFlagEnemy()
    {
        return BehaviorState.Success;
    }

    public BehaviorState SummonSkeletons()
    {
        return BehaviorState.Success;
    }

    public BehaviorState BuffAlliesHealNearby()
    {
        return BehaviorState.Success;
    }
}
