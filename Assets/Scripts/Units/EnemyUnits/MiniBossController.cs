using BehaviorTree;
using System.Collections; // Added to resolve IEnumerator error
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MiniBossController : MonoBehaviour
{
    [Header("MiniBoss Settings")]
    public MiniBossBlackboard blackboard;

    private MiniBossBT _bt;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private Animator _animator;

    private void Awake()
    {
        blackboard.Initialize(transform);
        _rb = GetComponent<Rigidbody2D>();
        _col= GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();
        StartCoroutine(DetectionUpdate());
    }

    private void Start()
    {
        _bt = this.GetComponent<MiniBossBT>();
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
        return blackboard.IsHealthBelowPercent(0.2f);
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

    public bool TargetIsAlive()
    {
        return blackboard.currentTarget != null && blackboard.currentTarget.gameObject.activeInHierarchy;
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
        if (blackboard.currentTarget == null)
        {
            return BehaviorState.Failure; // No target to attack
        }

        // Calculate the distance to the target
        float distanceToTarget = Vector2.Distance(transform.position, blackboard.currentTarget.position);

        // Check if the target is within attack range
        if (distanceToTarget > blackboard.darkBoltRange)
        {
            // Move closer to the target
            //Vector2 directionToTarget = (blackboard.currentTarget.position - transform.position).normalized;
            //Vector2 newPosition = Vector2.MoveTowards(transform.position, blackboard.currentTarget.position, blackboard.moveSpeed * Time.deltaTime);
            //transform.position = newPosition;
            MoveTo(blackboard.currentTarget.position + 6 * (blackboard.isFacingRight ? Vector3.right : Vector3.left));
            _animator.SetBool("IsMoving", true);
            return BehaviorState.Running; // Still moving to the target
        }

        // Check if the miniboss is facing the target
        if ((blackboard.currentTarget.position.x < transform.position.x && blackboard.isFacingRight) ||
            (blackboard.currentTarget.position.x > transform.position.x && !blackboard.isFacingRight))
        {
            Flip(); // Flip to face the target
        }

        // Stop moving and cast the Dark Bolt
        _animator.SetBool("IsMoving", false);
        _animator.SetTrigger("Cast_DarkBolt"); // Trigger the animation frame for casting
        blackboard.lastDarkBoltTime = Time.time;

        return BehaviorState.Success;
    }

    public BehaviorState SummonSkeletonsUntilLimit()
    {
        int maxArmySize = 12; // Define maxArmySize locally
        if (EnemyManager.Instance.GetActiveEnemyCount() >= maxArmySize)
        {
            return BehaviorState.Failure; // Cannot summon more if at max capacity
        }
        blackboard.lastSummonTime = Time.time;
        StartCoroutine(SummonSkeletonsCoroutine());
        return BehaviorState.Running;
    }

    private IEnumerator SummonSkeletonsCoroutine()
    {
        Debug.Log("Summoning All of Skeletons...");
        int maxArmySize = 12; // Define maxArmySize locally
        int skeletonsToSummon = Mathf.Min(Random.Range(2, blackboard.skeletonsPerSummon + 1), maxArmySize - EnemyManager.Instance.GetActiveEnemyCount());

        for (int i = 0; i < skeletonsToSummon; i++)
        {
            // Generate a random position around the mini-boss within the summon range (1f to 5f)
            Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(1f, 5f);
            Vector3 summonPosition = transform.position + new Vector3(randomOffset.x, 0, 0);

            // Instantiate the skeleton at the random position
            GameObject skeleton = Instantiate(blackboard.skeletonPrefab, summonPosition, Quaternion.identity);

            // Register the skeleton in the EnemyManager
            EnemyManager.Instance.RegisterEnemy(skeleton);

            // Wait for 0.2 seconds before summoning the next skeleton
            yield return new WaitForSeconds(0.2f);
        }

        yield return null;
    }

    public BehaviorState BuffAlliesDamageBoost()
    {

        return BehaviorState.Success;
    }

    public BehaviorState CommandArmyMoveToNextArea()
    {
        return BehaviorState.Success;
    }

    public BehaviorState SummonSkeletons()
    {
        int maxArmySize = 12; // Define maxArmySize locally
        if (EnemyManager.Instance.GetActiveEnemyCount() >= maxArmySize)
        {
            return BehaviorState.Failure; // Cannot summon more if at max capacity
        }
        blackboard.lastSummonTime = Time.time;
        StartCoroutine(SummonLimitSkeletonsCoroutine());
        return BehaviorState.Running;
    }
    private IEnumerator SummonLimitSkeletonsCoroutine()
    {
        Debug.Log("Summoning Limit Number of Skeletons...");
        int maxArmySize = 12; // Define maxArmySize locally
        int skeletonsToSummon = Mathf.Min(Random.Range(2, blackboard.skeletonsPerSummon + 1), 
            maxArmySize - EnemyManager.Instance.GetActiveEnemyCount()); // Random number of skeletons to summon

        for (int i = 0; i < skeletonsToSummon; i++)
        {
            _animator.SetTrigger("Cast_Spell_2");
            // Generate a random position around the mini-boss within the summon range (1f to 5f)
            Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(1f, 5f);
            Vector3 summonPosition = transform.position + new Vector3(randomOffset.x, 0, 0);

            GameObject skeleton = Instantiate(blackboard.skeletonPrefab, summonPosition, Quaternion.identity);

            // Wait for 0.5 seconds before summoning the next skeleton
            yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);
        }

        yield return null;
    }
    public BehaviorState BuffAlliesHealNearby()
    {
        _animator.SetTrigger("Cast_Spell_1");

        // Use the detected allies from DetectNearbyAllies
        List<GameObject> nearbyAllies = blackboard.nearbyAllies;

        // Sort allies by distance to the mini-boss
        nearbyAllies.Sort((a, b) => Vector2.Distance(transform.position, a.transform.position)
            .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        // Heal up to 5 closest allies
        int alliesToHeal = Mathf.Min(blackboard.numberAllyHealed, nearbyAllies.Count);
        for (int i = 0; i < alliesToHeal; i++)
        {
            Enemy ally = nearbyAllies[i].GetComponent<Enemy>();
            if (ally != null)
            {
                ally.Heal(blackboard.buffHealAmount);
            }
        }

        return BehaviorState.Success;
    }

    public void ShootDarkBolt()
    {
        if (blackboard.darkBoltPrefab != null && blackboard.darkBoltSpawnPoint != null)
        {
            Vector2 dir = blackboard.isFacingRight ? Vector2.right : Vector2.left;
            GameObject fireball = Instantiate(blackboard.darkBoltPrefab, blackboard.darkBoltSpawnPoint.position, Quaternion.identity);
            Fireball fireballCtrl = fireball.GetComponent<Fireball>();
            if (fireballCtrl != null)
            {
                Vector2 targetPosition = blackboard.currentTarget != null ? (Vector2)blackboard.currentTarget.position : (Vector2)transform.position + dir * 8f;
                Debug.Log(blackboard.darkBoltSpawnPoint.position + " " + targetPosition);
                fireballCtrl.Launch(blackboard.darkBoltSpawnPoint.position, targetPosition);
            }
        }
    }

    #region Helper Methods
    public void ImpulseDarkBolt()
    {
        AbilitiesScript abilitiesScript = blackboard.darkBoltPrefab.GetComponent<AbilitiesScript>();
        if (abilitiesScript != null && blackboard.currentTarget != null)
        {
            Vector2 direction = (blackboard.currentTarget.position - blackboard.darkBoltSpawnPoint.position).normalized;
            GameObject darkBolt = Instantiate(blackboard.darkBoltPrefab, blackboard.darkBoltSpawnPoint.position, Quaternion.identity);
            abilitiesScript = darkBolt.GetComponent<AbilitiesScript>();
            if (abilitiesScript != null)
            {
                abilitiesScript.Launch(direction);
            }
        }
    }
    private IEnumerator DetectionUpdate()
    {
        while (true)
        {
            DetectEnemyOnSight();

            yield return new WaitForSeconds(0.1f); // Update 10 times per second
        }
    }
    void DetectEnemyOnSight()
    {
        Collider2D[] players = Physics2D.OverlapCircleAll(transform.position,
            blackboard.detectionRange, blackboard.playerLayer);

        // Detect allies
        Collider2D[] enemyAllies = Physics2D.OverlapCircleAll(transform.position,
            blackboard.detectionRange, blackboard.allyLayer);

        // Combine detected players and allies
        List<Transform> detectedTargets = new List<Transform>();

        foreach (var player in players)
        {
            if ((blackboard.playerLayer & (1 << player.gameObject.layer)) != 0)
            {
                detectedTargets.Add(player.transform);
            }
        }
        blackboard.detectedEnemies = detectedTargets;
        foreach (var ally in enemyAllies)
        {
            if ((blackboard.allyLayer & (1 << ally.gameObject.layer)) != 0)
            {
                detectedTargets.Add(ally.transform);
            }
        }
        // Set the first detected target as the current target
        if (detectedTargets.Count > 0)
        {
            blackboard.currentTarget = detectedTargets[0];
        }
        if(detectedTargets.Count == 0)
        {
            blackboard.detectedEnemies.Clear();
        }
    }
    void MoveTo(Vector2 targetPos)
    {
        _animator.SetBool("IsMoving", true);
        if ((targetPos.x < transform.position.x && blackboard.isFacingRight) || (targetPos.x > transform.position.x && !blackboard.isFacingRight))
        {
            Flip();
        }
        Vector2 newPos = Vector2.MoveTowards(_rb.position, targetPos, blackboard.moveSpeed * Time.deltaTime);
    }
    void Flip()
    {
        blackboard.isFacingRight = !blackboard.isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    public void TakeDamage(float damage)
    {
        blackboard.hp -= damage;
        if(blackboard.hp <= 0)
        {
            _animator.SetTrigger("Death");
        } else
        {
            _animator.SetTrigger("Take_Dmg");
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var player = collision.gameObject.GetComponent<PlayerController>();
            player.TakeDamage(blackboard.damage, _rb.transform.position);
        }
        if (collision.gameObject.CompareTag("Ally"))
        {
            var ally = collision.gameObject.GetComponent<AllyUnitController>();
            ally.TakeDamage(blackboard.damage, _rb.transform.position);
        }
    }
    public void DetectNearbyAllies(float detectionRadius)
    {
        List<GameObject> nearbyAllies = new List<GameObject>();

        // Find all colliders within the detection radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        foreach (var collider in colliders)
        {
            // Check if the collider has the "Enemy" tag or is in the "Attackable" layer
            if (collider.CompareTag("Enemy") || (blackboard.allyLayerMask & (1 << collider.gameObject.layer)) != 0)
            {
                nearbyAllies.Add(collider.gameObject);
            }
        }

        blackboard.nearbyAllies = nearbyAllies;
    }
    private void OnDrawGizmos()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, blackboard.detectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blackboard.darkBoltRange);
    }
    public BehaviorState FaceTarget()
    {
        if (blackboard.currentTarget == null)
        {
            return BehaviorState.Failure;
        }

        if ((blackboard.currentTarget.position.x < transform.position.x && blackboard.isFacingRight) ||
            (blackboard.currentTarget.position.x > transform.position.x && !blackboard.isFacingRight))
        {
            Flip();
        }

        return BehaviorState.Success;
    }
    #endregion
}
