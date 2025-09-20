using UnityEngine;
using BehaviorTree;

public class MiniBossController : MonoBehaviour
{
    public EnemyBlackboard blackboard;

    private MiniBossBT _bt;

    private void Awake()
    {
        blackboard = new EnemyBlackboard();
        blackboard.hpMax = 200;
        blackboard.hp = 200;
        blackboard.hpLowThreshold = 50;
        blackboard.waveActive = true;
    }

    private void Start()
    {
        _bt = new MiniBossBT(blackboard, this);
    }

    private void Update()
    {
        _bt.Tick();
    }

    public void Spawn()
    {
    }

    public BehaviorState MoveForward()
    {
        transform.Translate(Vector3.right * Time.deltaTime);
        return BehaviorState.Running;
    }

    public BehaviorState Attack(Transform target)
    {
        if (target == null) return BehaviorState.Failure;
        return BehaviorState.Success;
    }

    public BehaviorState MoveToBaseSpawn()
    {
        // TODO: Move to spawn/base position
        return BehaviorState.Running;
    }

    public BehaviorState HealAtSpawn()
    {
        // TODO: Heal logic
        return BehaviorState.Success;
    }

    public BehaviorState SummonUnitsUntilLimit(int limit)
    {
        // TODO: Summon logic
        return BehaviorState.Success;
    }

    public BehaviorState ReduceCooldowns()
    {
        // TODO: Reduce cooldowns logic
        return BehaviorState.Success;
    }

    public BehaviorState ForceAdvanceToNextArea()
    {
        // TODO: Advance logic
        return BehaviorState.Success;
    }

    public BehaviorState CastDarkBoltAtTarget()
    {
        // TODO: Cast Dark Bolt logic
        return BehaviorState.Success;
    }

    public BehaviorState SummonSkeletonsUntilLimit(int limit)
    {
        // TODO: Summon skeletons logic
        return BehaviorState.Success;
    }

    public BehaviorState BuffAlliesDamageBoost()
    {
        // TODO: Buff damage logic
        return BehaviorState.Success;
    }

    public BehaviorState CommandArmyMoveToNextArea()
    {
        // TODO: Command army logic
        return BehaviorState.Success;
    }

    public BehaviorState SetFlagEnemy()
    {
        // TODO: Set flag logic
        return BehaviorState.Success;
    }

    public BehaviorState SummonSkeletons()
    {
        // TODO: Summon skeletons logic
        return BehaviorState.Success;
    }

    public BehaviorState BuffAlliesHealNearby()
    {
        // TODO: Heal nearby allies logic
        return BehaviorState.Success;
    }

    public void BuffAllies()
    {
        float buffRadius = 5f;  // Buff range, can modify in Inspector
        float healAmount = 20f; // Amount of health restored for each small monster 
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, buffRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") && hit.gameObject != this.gameObject)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.Heal(healAmount);
                }
            }
        }
        Debug.Log("MiniBoss buffed nearby allies.");
    }

    public void Enrage()
    {
        if (!blackboard.isEnraged)
        {
            blackboard.isEnraged = true;
            //TODO: Enrage logic (e.g., increase attack speed, damage, etc.)
        }
    }

    public void Retreat()
    {
        //TODO: Retreat logic
    }
}
