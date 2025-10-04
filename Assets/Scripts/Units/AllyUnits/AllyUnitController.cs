using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public class AllyUnitController : MonoBehaviour
{
    // Blackboard for storing unit state and references
    private AllyBlackboard blackboard;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool facingRight = true;

    [Header("Vision")]
    [SerializeField] private Transform visionOrigin;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int rayCount = 8; // Number of rays to cast within field of view
    [SerializeField] private float viewDistance = 8f; // How far unit can see
    [SerializeField] private bool showVisionGizmos = true; // For debugging

    [Header("Ranged Attack")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;

    // Event to warn allies
    public delegate void WarnEvent(Vector2 enemyPosition);
    public static event WarnEvent OnWarn;

    private float lastShootTime = -10f;
    [SerializeField] private float shootCooldown = 1.5f;

    private bool isFollowingPlayer = false;
    private Transform followTargetPlayer = null;

    private bool waitingAtPatrolPoint = false;
    private float patrolWaitTimer = 0f;
    private float patrolWaitDuration = 0f;

    void Start()
    {
        blackboard = GetComponent<AllyBlackboard>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Follow player logic
        if (isFollowingPlayer && followTargetPlayer != null && !blackboard.isEngaging && !blackboard.isWarning)
        {
            float dist = Vector2.Distance(transform.position, followTargetPlayer.position);
            if (dist > 2.5f)
            {
                float dirX = Mathf.Sign(followTargetPlayer.position.x - transform.position.x);
                rb.velocity = new Vector2(dirX * blackboard.walkSpeed, 0);
                animator.SetBool("IsMoving", true);
                if (dirX > 0 && !facingRight || dirX < 0 && facingRight)
                {
                    Flip();
                }
            }
            else
            {
                rb.velocity = Vector2.zero;
                animator.SetBool("IsMoving", false);
            }
        }

        // Patrol wait logic
        if (waitingAtPatrolPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            rb.velocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            if (patrolWaitTimer >= patrolWaitDuration)
            {
                waitingAtPatrolPoint = false;
                patrolWaitTimer = 0f;
            }
        }
    }

    public void FollowPlayer(Transform player)
    {
        isFollowingPlayer = true;
        followTargetPlayer = player;
        blackboard.isPatrolling = false;
        rb.velocity = Vector2.zero;
        animator.SetBool("IsMoving", false);
    }

    public void CancelFollowPlayer()
    {
        isFollowingPlayer = false;
        followTargetPlayer = null;
        blackboard.isPatrolling = true;
        rb.velocity = Vector2.zero;
        animator.SetBool("IsMoving", false);
    }

    public bool Patrol()
    {
        if (waitingAtPatrolPoint)
        {
            return false;
        }

        // Patrol between points
        if (blackboard.patrolPoints == null || blackboard.patrolPoints.Length == 0)
            return true;

        Transform targetPoint = blackboard.patrolPoints[blackboard.currentPatrolIndex];
        Vector2 directionToTarget = (targetPoint.position - transform.position).normalized;

        // Check if we need to flip the sprite
        if (directionToTarget.x > 0 && !facingRight || directionToTarget.x < 0 && facingRight)
        {
            Flip();
        }
        // Move towards patrol point with fixed speed (avoid slowing down near target)
        float moveSpeed = blackboard.walkSpeed;
        rb.velocity = new Vector2(Mathf.Sign(targetPoint.position.x - transform.position.x) * moveSpeed, 0);
        animator.SetBool("IsMoving", true);

        // Check if we've reached the current patrol point
        if (Vector2.Distance(transform.position, targetPoint.position) <= 2.2f)
        {
            rb.velocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            blackboard.currentPatrolIndex = (blackboard.currentPatrolIndex + 1) % blackboard.patrolPoints.Length;
            waitingAtPatrolPoint = true;
            patrolWaitDuration = Random.Range(1f, 3f);
            patrolWaitTimer = 0f;
            return false;
        }
        CheckVision();
        return false;
    }

    public void PlayIdleAnimation()
    {
        animator.SetBool("IsMoving", false);
    }

    public bool WarnAction()
    {
        // Move to warn position if not already there, move slow as half of normal speed, stop at warn position 2 distance long
        if (blackboard.detectedEnemies.Count > 0)
        {
            Vector2 enemyPos = blackboard.detectedEnemies[0].position;
            OnWarn?.Invoke(enemyPos);
            if (!blackboard.isWarning)
            {
                Debug.Log("called");
                blackboard.isWarning = true;
            }
                
            
            // Set current target to closest enemy for next engage phase
            Transform closestEnemy = null;
            float minDist = float.MaxValue;
            foreach (var enemy in blackboard.detectedEnemies)
            {
                if (enemy == null) continue;
                float dist = Vector2.Distance(transform.position, enemy.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestEnemy = enemy;
                }
            }
            blackboard.currentTarget = closestEnemy;
        }
        
        if (blackboard.isWarning && blackboard.warnedEnemyPosition != Vector2.zero)
        {
            Vector2 dir = (blackboard.warnedEnemyPosition - (Vector2)transform.position).normalized;
            float dist = Vector2.Distance(transform.position, blackboard.warnedEnemyPosition);
            float stopDist = 2f;
            
            if (dist > stopDist)
            {
                float dirX = Mathf.Sign(blackboard.warnedEnemyPosition.x - transform.position.x);
                rb.velocity = new Vector2(dirX * (blackboard.walkSpeed), 0);
                animator.SetBool("IsMoving", true);
                if (dirX > 0 && !facingRight || dirX < 0 && facingRight)
                {
                    Flip();
                }
                return false;
            }
            else
            {
                rb.velocity = Vector2.zero;
                animator.SetBool("IsMoving", false);
                
                // If no enemy detected anymore, back to patrol
                if (blackboard.detectedEnemies.Count == 0)
                {
                    blackboard.ResetWarnState();
                    blackboard.currentTarget = null;
                    return true;
                }
                return true;
            }
        }
        return false;
    }

    public bool EngageAction()
    {
        // Clean up destroyed or missing enemies
        blackboard.detectedEnemies.RemoveAll(enemy =>
            enemy == null || Vector2.Distance(transform.position, enemy.position) > viewDistance);

        // If no enemies detected but we have a current target, try to pursue
        if (blackboard.detectedEnemies.Count == 0 && blackboard.currentTarget != null)
        {
            // Start pursuing if target still exists but out of sight
            if (blackboard.currentTarget.gameObject.activeInHierarchy)
            {
                if(blackboard.currentTarget.position != Vector3.zero)
                    blackboard.lastKnownEnemyPosition = blackboard.currentTarget.position;
                blackboard.isPursuing = true;
                blackboard.searchTimer = 0f;
                blackboard.ResetWarnState(); // Reset warning when starting pursuit
                return true; // Switch to PursueAction
            }
            else
            {
                // Target is destroyed, reset to patrol
                blackboard.ResetEngageState();
                blackboard.ResetWarnState();
                rb.velocity = Vector2.zero;
                animator.SetBool("IsMoving", false);
                return true;
            }
        }
        
        // If no enemies detected and no current target, reset to patrol
        if (blackboard.detectedEnemies.Count == 0)
        {
            blackboard.currentTarget = null;
            blackboard.ResetEngageState();
            blackboard.ResetWarnState();
            rb.velocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            return true;
        }
        
        // Find closest enemy in range (distance only on x axis)
        Transform target = null;
        float minDist = float.MaxValue;  
        foreach (var enemy in blackboard.detectedEnemies)
        {
            if (enemy == null || enemy.Equals(null)) continue;
            float dist = Mathf.Abs(enemy.position.x - transform.position.x);
            if (dist < minDist && dist <= blackboard.engageRange)
            {
                minDist = dist;
                target = enemy;
            }
        }
        blackboard.currentTarget = target;
        
        if (blackboard.currentTarget == null || blackboard.currentTarget.Equals(null) ||
            !blackboard.currentTarget.gameObject.activeInHierarchy)
        {
            blackboard.currentTarget = null;
            blackboard.ResetEngageState();
            blackboard.ResetWarnState();
            rb.velocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            return true;
        }
        
        // Check if target is too far, start pursuing instead of giving up immediately
        float maxChaseDist = blackboard.chaseRadius;
        float distToTarget = Vector2.Distance(transform.position, target.position);
        if (distToTarget > maxChaseDist)
        {
            // Instead of giving up, start pursuing
            Debug.Log(blackboard.currentTarget.position);
            blackboard.lastKnownEnemyPosition = blackboard.currentTarget.position;
            blackboard.isPursuing = true;
            blackboard.searchTimer = 0f;
            blackboard.ResetWarnState(); // Reset warning when starting pursuit
            return true; // Switch to PursueAction
        }
        
        Vector2 dirToTarget = new Vector2((target.position - transform.position).normalized.x, 0);
        float distToTargetX = Mathf.Abs(target.position.x - transform.position.x);
        
        if (dirToTarget.x > 0 && !facingRight || dirToTarget.x < 0 && facingRight)
        {
            Flip();
        }
        
        switch (blackboard.combatType)
        {
            case AllyCombatType.Melee:
                if (distToTargetX > 1.2f)
                {
                    rb.velocity = new Vector2(Mathf.Sign(target.position.x - transform.position.x) * blackboard.walkSpeed, 0);
                    animator.SetBool("IsMoving", true);
                }
                else
                {
                    rb.velocity = Vector2.zero;
                    animator.SetBool("IsMoving", false);
                    animator.SetTrigger("Attack");
                    if (Random.value < 0.2f)
                        animator.SetTrigger("Block");
                }
                break;
                
            case AllyCombatType.Ranged:
                float safeDist = 6f;
                if (distToTargetX < safeDist && !animator.GetCurrentAnimatorStateInfo(0).IsName("Archer_attack"))
                {
                    float runDir = Mathf.Sign(transform.position.x - target.position.x);
                    rb.velocity = new Vector2(runDir * blackboard.walkSpeed, 0);
                    animator.SetBool("IsMoving", true);
                    if (runDir > 0 && !facingRight || runDir < 0 && facingRight)
                    {
                        Flip();
                    }
                }
                else if (distToTargetX <= blackboard.engageRange)
                {
                    rb.velocity = Vector2.zero;
                    animator.SetBool("IsMoving", false);
                    if ((dirToTarget.x > 0 && !facingRight) || (dirToTarget.x < 0 && facingRight))
                    {
                        Flip();
                    }
                    if (Time.time >= lastShootTime + shootCooldown)
                    {
                        animator.SetTrigger("Attack");
                        lastShootTime = Time.time;
                    }
                }
                break;
                
            case AllyCombatType.Support:
                Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, blackboard.supportRange);
                foreach (var col in allies)
                {
                    if (col.CompareTag("Ally") && col.gameObject != this.gameObject)
                    {
                        animator.SetTrigger("Buff");
                        break;
                    }
                }
                rb.velocity = Vector2.zero;
                animator.SetBool("IsMoving", false);
                break;
        }
        return false;
    }

    public bool PursueAction()
    {        
        // Initialize pursuit if not already pursuing
        if (!blackboard.isPursuing && blackboard.currentTarget != null)
        {
            blackboard.isPursuing = true;
            blackboard.searchTimer = 0f;
        }
        // If target is found again, go back to engage
        if (blackboard.currentTarget != null && blackboard.detectedEnemies.Contains(blackboard.currentTarget))
        {
            blackboard.ResetPursueState();
            return true; // Back to engage
        }
        // If no current target, search at last known position
        if (blackboard.currentTarget == null || !blackboard.currentTarget.gameObject.activeInHierarchy)
        {
            blackboard.searchTimer += Time.deltaTime;
            
            Vector2 dir = (blackboard.lastKnownEnemyPosition - (Vector2)transform.position).normalized;
            float dist = Vector2.Distance(transform.position, blackboard.lastKnownEnemyPosition);
            
            if (dist > 0.5f)
            {
                float dirX = Mathf.Sign(blackboard.lastKnownEnemyPosition.x - transform.position.x);
                rb.velocity = new Vector2(dirX * blackboard.walkSpeed, 0);
                animator.SetBool("IsMoving", true);
                
                if (dirX > 0 && !facingRight || dirX < 0 && facingRight)
                {
                    Flip();
                }
            }
            else
            {
                rb.velocity = Vector2.zero;
                animator.SetBool("IsMoving", false);
            }
            
            // Give up after max search time
            if (blackboard.searchTimer > blackboard.maxSearchTime)
            {
                blackboard.ResetPursueState();
                return true; // Back to patrol
            }
            return false;
        }
        
        // Check if we're too far from patrol area
        if (blackboard.patrolPoints != null && blackboard.patrolPoints.Length > 0 && blackboard.lastKnownEnemyPosition == Vector2.zero)
        {
            float distFromStart = Vector2.Distance(transform.position, blackboard.patrolPoints[0].position);
            if (distFromStart > blackboard.chaseRadius)
            {
                blackboard.ResetPursueState();
                return true; // Back to patrol
            }
        }
        // Move towards current target
        float dirX2 = Mathf.Sign(blackboard.currentTarget.position.x - transform.position.x);
        rb.velocity = new Vector2(dirX2 * blackboard.walkSpeed, 0);
        animator.SetBool("IsMoving", true);
        
        float distToTarget = Vector2.Distance(transform.position, blackboard.currentTarget.position);
        if (dirX2 > 0 && !facingRight || dirX2 < 0 && facingRight)
        {
            Flip();
        }
        
        // If close enough to engage, switch back
        if (distToTarget <= blackboard.engageRange)
        {
            blackboard.ResetPursueState();
            return true; // Back to engage
        }
        
        return false;
    }

    public bool SurrenderAction()
    {
        // Only run away to a safe zone, do not perform last stand or other surrender actions
        if (blackboard.surrenderTargetBase != null)
        {
            float dirX = Mathf.Sign(blackboard.surrenderTargetBase.position.x - transform.position.x);
            rb.velocity = new Vector2(dirX * blackboard.walkSpeed * 1.2f, 0);
            animator.SetBool("IsMoving", true);
            float dist = Vector2.Distance(transform.position, blackboard.surrenderTargetBase.position);
            if (dirX > 0 && !facingRight || dirX < 0 && facingRight)
            {
                Flip();
            }
            if (dist > 0.5f)
            {
                rb.velocity = new Vector2(dirX, 0) * blackboard.walkSpeed * 1.2f;
                animator.SetBool("IsMoving", true);
            }
            else
            {
                rb.velocity = Vector2.zero;
                animator.SetBool("IsMoving", false);
            }
        }
        else
        {
            rb.velocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
        }
        return false;
        // Commented out: last stand and other surrender logic
        // float hpRatio = 1.0f;
        // if (blackboard != null && blackboard.maxHP > 0)
        // {
        //     hpRatio = blackboard.currentHP / blackboard.maxHP;
        // }
        // if (!blackboard.isSurrendering && !blackboard.isLastStand)
        // {
        //     float roll = Random.value;
        //     if (roll < blackboard.lastStandChance)
        //     {
        //         blackboard.isLastStand = true;
        //         blackboard.surrenderType = SurrenderType.LastStand;
        //     }
        //     else
        //     {
        //         blackboard.isSurrendering = true;
        //         blackboard.surrenderType = SurrenderType.Surrender;
        //     }
        // }
        // if (blackboard.isLastStand) { ... }
    }

    private void OnEnable()
    {
        OnWarn += HandleWarnSignal;
    }
    private void OnDisable()
    {
        OnWarn -= HandleWarnSignal;
    }
    private void HandleWarnSignal(Vector2 enemyPosition)
    {
        if (Vector2.Distance(transform.position, enemyPosition) <= blackboard.detectionRange * 2)
        {
            blackboard.ReceiveWarnSignal(enemyPosition);
        }
    }

    private void CheckVision()
    {
        // Vision cone raycast for enemy detection
        if (visionOrigin == null) return;
        
        // Remove enemies that are too far or destroyed
        blackboard.detectedEnemies.RemoveAll(enemy =>
            enemy == null || Vector2.Distance(transform.position, enemy.position) > viewDistance);

        float startAngle = facingRight ? -blackboard.fieldOfView / 2 : 180 - blackboard.fieldOfView / 2;
        float endAngle = facingRight ? blackboard.fieldOfView / 2 : 180 + blackboard.fieldOfView / 2;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)(rayCount - 1));
            Vector2 direction = GetVectorFromAngle(angle);
            
            RaycastHit2D hit = Physics2D.Raycast(
                visionOrigin.position,
                direction,
                viewDistance,
                enemyLayer | obstacleLayer
            );
            
            if (hit.collider != null)
            {
                if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
                {
                    Transform enemy = hit.transform;
                    if (!blackboard.detectedEnemies.Contains(enemy))
                    {
                        blackboard.detectedEnemies.Add(enemy);                        
                        blackboard.isWarning = true;
                        blackboard.warnedEnemyPosition = enemy.position;
                    }
                }
            }
        }
    }

    private Vector2 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    private void OnDrawGizmos()
    {
        // Draw vision cone and detected enemies for debugging
        if (!showVisionGizmos || visionOrigin == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(visionOrigin.position, 0.2f);
        float fieldOfView = 90f;
        if (blackboard != null)
            fieldOfView = blackboard.fieldOfView;
        float startAngle = facingRight ? -fieldOfView / 2 : 180 - fieldOfView / 2;
        float endAngle = facingRight ? fieldOfView / 2 : 180 + fieldOfView / 2;
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)(rayCount - 1));
            Vector2 direction = GetVectorFromAngle(angle);
            Gizmos.DrawRay(visionOrigin.position, direction * viewDistance);
        }
        if (blackboard != null && blackboard.detectedEnemies != null)
        {
            Gizmos.color = Color.red;
            foreach (var enemy in blackboard.detectedEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(visionOrigin.position, enemy.position);
                    Gizmos.DrawWireSphere(enemy.position, 0.5f);
                }
            }
        }
    }

    private void Flip()
    {
        // Flip sprite horizontally
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void ShootArrow()
    {
        // Only instantiate arrow, do not check cooldown here
        if (arrowPrefab != null && arrowSpawnPoint != null)
        {
            Vector2 dir = facingRight ? Vector2.right : Vector2.left;
            GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.identity);
            ArrowController arrowCtrl = arrow.GetComponent<ArrowController>();
            if (arrowCtrl != null)
            {
                arrowCtrl.Init(dir, blackboard.currentTarget);
            }
        }
    }
    public void TakeDamage(float damage, Vector2 attackPos)
    {
        // Apply damage to this ally unit
        if (blackboard != null && blackboard.currentHP > 0)
        {
            blackboard.currentHP -= Mathf.RoundToInt(damage);
            animator.SetTrigger("Hurt");
            if (blackboard.currentHP <= 0)
            {
                animator.SetTrigger("Death");
                return;
            }
        }
    }
    public void DestroySelf()
    {
        // Destroy this unit
        Destroy(gameObject);
    }
}
