using UnityEngine;
using BehaviorTree;

public class MiniBossController : MonoBehaviour
{
    [Header("MiniBoss Settings")]
    public EnemyBlackboard blackboard;
    
    [Header("Flip Settings")]
    [HideInInspector] public bool facingRight = true;  // True if facing right, false if facing left
    
    [Header("Prefabs & References")]
    public GameObject[] skeletonPrefabs; // Assign skeleton prefabs here
    public GameObject darkBoltPrefab;    // Assign dark bolt projectile prefab
    public LayerMask enemyDetectionLayer = -1; // Layer for Player and Ally units
    public LayerMask allyDetectionLayer = -1;  // Layer for Enemy units
    
    [Header("Audio & Effects")]
    public AudioClip[] spellSounds;      // Sound effects for spells
    public ParticleSystem[] spellEffects; // Visual effects for spells
    
    private MiniBossBT _bt;
    private AudioSource _audioSource;

    private void Awake()
    {
        // Use MiniBossBlackboard for better functionality
        blackboard = new MiniBossBlackboard();
        _audioSource = GetComponent<AudioSource>();
        
        // Initialize flip state based on current localScale
        facingRight = transform.localScale.x > 0;
        
        // Initialize spawn position if spawn point is available
        if (blackboard.spawnPoint != null)
        {
            blackboard.spawnPosition = blackboard.spawnPoint.transform.position;
        }
        else
        {
            blackboard.spawnPosition = transform.position;
        }
        
        blackboard.isAtSpawn = true;
        blackboard.gameStartTime = Time.time;
        
        // Start detection coroutine
        StartCoroutine(DetectionUpdate());
    }

    private void Start()
    {
        _bt = new MiniBossBT(blackboard, this);
    }

    private void Update()
    {
        // Update current army size by counting nearby allies
        UpdateArmySize();
        
        // Update position-based flags
        UpdatePositionFlags();
        
        // Update frenzy state
        UpdateFrenzyState();
        
        // Auto flip to face target
        AutoFlipToTarget();
        
        _bt.Tick();
    }

    // Auto flip to face target
    private void AutoFlipToTarget()
    {
        if (blackboard.targetPlayer != null)
        {
            Vector3 targetDirection = blackboard.targetPlayer.position - transform.position;
            
            // Flip if target is on the opposite side
            if (targetDirection.x < 0 && facingRight)
            {
                Flip();
            }
            else if (targetDirection.x > 0 && !facingRight)
            {
                Flip();
            }
        }
    }
    public void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    /// <summary>
    /// Flip to face specific target position
    /// </summary>
    /// <param name="targetPosition">Position to face</param>
    public void FlipToFaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        
        // Only flip if we need to
        if ((direction.x > 0 && !facingRight) || (direction.x < 0 && facingRight))
        {
            Flip();
        }
    }

    /// <summary>
    /// Check if MiniBoss is facing the target
    /// </summary>
    /// <param name="targetPosition">Target position to check</param>
    /// <returns>True if facing target</returns>
    public bool IsFacingTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        return (direction.x > 0 && facingRight) || (direction.x < 0 && !facingRight);
    }

    // Detection system to update blackboard
    private System.Collections.IEnumerator DetectionUpdate()
    {
        while (true)
        {
            UpdateDetection();
            yield return new WaitForSeconds(0.2f); // Update 5 times per second
        }
    }

    private void UpdateDetection()
    {
        // Find nearby allies (other enemies)
        blackboard.nearbyAllies.Clear();
        Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, blackboard.detectionRange, allyDetectionLayer);
        foreach (var ally in allies)
        {
            if (ally.transform != transform) // Don't include self
            {
                blackboard.nearbyAllies.Add(ally.transform);
            }
        }

        // Find detected enemies (player and ally units)
        blackboard.detectedEnemies.Clear();
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, blackboard.detectionRange, enemyDetectionLayer);
        foreach (var enemy in enemies)
        {
            blackboard.detectedEnemies.Add(enemy.transform);
            
            // Set target player if found
            if (enemy.CompareTag("Player"))
            {
                blackboard.targetPlayer = enemy.transform;
            }
        }
    }

    // Helper methods to maintain blackboard state
    private void UpdateArmySize()
    {
        blackboard.currentArmySize = blackboard.nearbyAllies.Count;
    }

    private void UpdatePositionFlags()
    {
        if (blackboard.spawnPoint != null)
        {
            float distanceToSpawn = Vector3.Distance(transform.position, blackboard.spawnPosition);
            blackboard.isAtSpawn = distanceToSpawn < 2f;
        }
    }

    private void UpdateFrenzyState()
    {
        if (blackboard.isFrenzyActive)
        {
            if (Time.time > blackboard.frenzyStartTime + blackboard.frenzyDuration)
            {
                blackboard.isFrenzyActive = false;
            }
        }
    }

    // --- Action Methods for Behavior Tree ---
    
    public void Spawn()
    {
        blackboard.isAtSpawn = true;
        blackboard.hp = blackboard.hpMax;
    }

    public BehaviorState MoveForward()
    {
        // Move in facing direction
        float direction = facingRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * blackboard.moveSpeed * Time.deltaTime);
        return BehaviorState.Running;
    }

    public BehaviorState Attack(Transform target)
    {
        if (target == null) return BehaviorState.Failure;
        
        // Face the target before attacking
        FlipToFaceTarget(target.position);
        
        blackboard.lastAttackTime = Time.time;
        return BehaviorState.Success;
    }

    public BehaviorState MoveToBaseSpawn()
    {
        if (blackboard.isAtSpawn) return BehaviorState.Success;
        
        Vector3 direction = (blackboard.spawnPosition - transform.position).normalized;
        
        // Face movement direction
        FlipToFaceTarget(blackboard.spawnPosition);
        
        transform.Translate(direction * blackboard.retreatSpeed * Time.deltaTime);
        
        float distance = Vector3.Distance(transform.position, blackboard.spawnPosition);
        return distance < 2f ? BehaviorState.Success : BehaviorState.Running;
    }

    public BehaviorState HealAtSpawn()
    {
        if (!blackboard.isAtSpawn) return BehaviorState.Failure;
        
        if (blackboard.hp < blackboard.hpMax)
        {
            blackboard.hp = Mathf.Min(blackboard.hpMax, 
                blackboard.hp + Mathf.RoundToInt(blackboard.healAtSpawnRate * Time.deltaTime));
            return BehaviorState.Running;
        }
        
        return BehaviorState.Success;
    }

    public BehaviorState SummonUnitsUntilLimit(int limit)
    {
        if (blackboard.currentArmySize >= limit) return BehaviorState.Success;
        if (!blackboard.CanSummon()) return BehaviorState.Failure;
        
        return SummonSkeletons();
    }

    public BehaviorState ReduceCooldowns()
    {
        if (!blackboard.isFrenzyActive)
        {
            blackboard.isFrenzyActive = true;
            blackboard.frenzyStartTime = Time.time;
            
            PlaySound(1); // Frenzy sound
            PlayEffect(1); // Frenzy effect
        }
        
        return BehaviorState.Success;
    }

    public BehaviorState ForceAdvanceToNextArea()
    {
        blackboard.hasCommandedArmy = true;
        blackboard.lastAreaAdvanceTime = Time.time;
        
        PlaySound(2); // Command sound
        return BehaviorState.Success;
    }

    public BehaviorState CastDarkBoltAtTarget()
    {
        if (!blackboard.CanCastDarkBolt() || blackboard.targetPlayer == null) 
            return BehaviorState.Failure;
        
        blackboard.lastDarkBoltTime = Time.time;
        
        // Face target before casting
        FlipToFaceTarget(blackboard.targetPlayer.position);
        
        // Create and fire dark bolt
        if (darkBoltPrefab != null)
        {
            Vector3 direction = (blackboard.targetPlayer.position - transform.position).normalized;
            Vector3 spawnPos = transform.position + direction * 1f;
            
            GameObject bolt = Instantiate(darkBoltPrefab, spawnPos, Quaternion.identity);
            
            // Setup projectile (assuming it has Rigidbody2D)
            Rigidbody2D rb = bolt.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * 12f; // Projectile speed
            }
            
            // Auto destroy after 5 seconds
            Destroy(bolt, 5f);
        }
        
        PlaySound(0); // Dark bolt sound
        PlayEffect(0); // Dark bolt effect
        
        Debug.Log("MiniBoss casts Dark Bolt!");
        return BehaviorState.Success;
    }

    public BehaviorState SummonSkeletonsUntilLimit(int limit)
    {
        return SummonUnitsUntilLimit(limit);
    }

    public BehaviorState BuffAlliesDamageBoost()
    {
        if (!blackboard.CanBuffAllies()) return BehaviorState.Failure;
        
        blackboard.lastBuffTime = Time.time;
        blackboard.hasBuffedAllies = true;
        
        // Apply damage boost to nearby allies
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, blackboard.buffRadius, allyDetectionLayer);
        foreach (var hit in hits)
        {
            if (hit.gameObject != this.gameObject)
            {
                // Apply buff effect - can extend with additional buff damage logic here
                Debug.Log($"Buffed ally: {hit.name}");
            }
        }
        
        PlaySound(3); // Buff sound
        PlayEffect(2); // Buff effect
        
        return BehaviorState.Success;
    }

    public BehaviorState CommandArmyMoveToNextArea()
    {
        if (blackboard.hasCommandedArmy) return BehaviorState.Success;
        
        blackboard.hasCommandedArmy = true;
        blackboard.lastAreaAdvanceTime = Time.time;
        
        // Command all nearby allies to advance
        foreach (var ally in blackboard.nearbyAllies)
        {
            if (ally != null)
            {
                // Send advance command to ally - can extend with additional AI command system
                Debug.Log($"Commanded {ally.name} to advance");
            }
        }
        
        return BehaviorState.Success;
    }

    public BehaviorState SetFlagEnemy()
    {
        blackboard.commandIssued = true;
        return BehaviorState.Success;
    }

    public BehaviorState SummonSkeletons()
    {
        if (!blackboard.CanSummon()) return BehaviorState.Failure;
        
        blackboard.lastSummonTime = Time.time;
        
        // Summon skeletons using prefabs
        for (int i = 0; i < blackboard.skeletonsPerSummon; i++)
        {
            if (skeletonPrefabs != null && skeletonPrefabs.Length > 0)
            {
                // Choose random skeleton type
                GameObject prefab = skeletonPrefabs[Random.Range(0, skeletonPrefabs.Length)];
                
                Vector3 spawnPos = transform.position + new Vector3(
                    Random.Range(-blackboard.summonRange, blackboard.summonRange), 
                    0, 
                    Random.Range(-blackboard.summonRange, blackboard.summonRange)
                );
                
                GameObject skeleton = Instantiate(prefab, spawnPos, Quaternion.identity);
                Debug.Log($"Summoned skeleton at {spawnPos}");
            }
            else
            {
                Debug.LogWarning("No skeleton prefabs assigned to MiniBossController!");
            }
        }
        
        PlaySound(4); // Summon sound
        PlayEffect(3); // Summon effect
        
        return BehaviorState.Success;
    }

    public BehaviorState BuffAlliesHealNearby()
    {
        if (!blackboard.CanBuffAllies()) return BehaviorState.Failure;
        
        BuffAllies(); // Use existing method
        return BehaviorState.Success;
    }

    public void BuffAllies()
    {
        if (!blackboard.CanBuffAllies()) return;
        
        blackboard.lastBuffTime = Time.time;
        blackboard.hasBuffedAllies = true;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, blackboard.buffRadius, allyDetectionLayer);
        foreach (var hit in hits)
        {
            if (hit.gameObject != this.gameObject)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.Heal(blackboard.buffHealAmount);
                }
            }
        }
        
        PlaySound(3); // Heal sound
        PlayEffect(2); // Heal effect
        
        Debug.Log("MiniBoss buffed nearby allies.");
    }

    public void Enrage()
    {
        if (!blackboard.isEnraged)
        {
            blackboard.isEnraged = true;
            blackboard.isFrenzyActive = true;
            blackboard.frenzyStartTime = Time.time;
        }
    }

    public void Retreat()
    {
        blackboard.isRetreating = true;
        // Will be handled by MoveToBaseSpawn action
    }

    // Helper methods
    private void PlaySound(int soundIndex)
    {
        if (_audioSource != null && spellSounds != null && soundIndex < spellSounds.Length && spellSounds[soundIndex] != null)
        {
            _audioSource.PlayOneShot(spellSounds[soundIndex]);
        }
    }

    private void PlayEffect(int effectIndex)
    {
        if (spellEffects != null && effectIndex < spellEffects.Length && spellEffects[effectIndex] != null)
        {
            spellEffects[effectIndex].Play();
        }
    }

    // Gizmos to visualize ranges in Scene view
    private void OnDrawGizmosSelected()
    {
        if (blackboard != null)
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, blackboard.detectionRange);
            
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, blackboard.attackRange);
            
            // Buff radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, blackboard.buffRadius);
            
            // Summon range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, blackboard.summonRange);
            
            // Draw facing direction
            Gizmos.color = Color.white;
            Vector3 facingDirection = facingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawRay(transform.position, facingDirection * 2f);
        }
    }
}
