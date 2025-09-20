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

    public bool InvestigateSound()
    {
        // Move to investigate sound source
        if (!blackboard.isInvestigatingSoundSource)
            return true;
        Vector2 directionToSound = (blackboard.lastSoundPosition - (Vector2)transform.position).normalized;

        // Turn towards the sound
        if (directionToSound.x > 0 && !facingRight || directionToSound.x < 0 && facingRight)
        {
            Flip();
        }

        // Move towards the sound source
        if (Vector2.Distance(transform.position, blackboard.lastSoundPosition) > 0.5f)
        {
            float dirX = Mathf.Sign(blackboard.lastSoundPosition.x - transform.position.x);
            rb.velocity = new Vector2(dirX * blackboard.walkSpeed, 0);
            animator.SetBool("IsMoving", true);
            return false;
        }

        // Reached the sound source
        rb.velocity = Vector2.zero;
        animator.SetBool("IsMoving", false);
        blackboard.ResetAlertState();
        return true;
    }

    public bool WarnAction()
    {
        // Move to warn position if not already there, move slow as half of normal speed, stop at warn position 2 distance long
        if (blackboard.detectedEnemies.Count > 0)
        {
            Vector2 enemyPos = blackboard.detectedEnemies[0].position;
            OnWarn?.Invoke(enemyPos);
            blackboard.isWarning = true;
            blackboard.warnedEnemyPosition = enemyPos;
        }
        if (blackboard.isWarning && blackboard.warnedEnemyPosition != Vector2.zero)
        {
            Vector2 dir = (blackboard.warnedEnemyPosition - (Vector2)transform.position).normalized;
            float dist = Vector2.Distance(transform.position, blackboard.warnedEnemyPosition);
            float stopDist = 2f;
            if (dist > stopDist)
            {
                float dirX = Mathf.Sign(blackboard.warnedEnemyPosition.x - transform.position.x);
                rb.velocity = new Vector2(dirX * (blackboard.walkSpeed * 0.5f), 0);
                animator.SetBool("IsMoving", true);
                // Flip sprite to face warn position
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
                    blackboard.isWarning = false;
                    blackboard.warnedEnemyPosition = Vector2.zero;
                    return true; // Back to patrol
                }
                // Else, do next move in code (engage, etc.)
                return false;
            }
        }
        return false;
    }

    public bool EngageAction()
    {
        // Panic if morale is too low
        if (blackboard.morale < blackboard.moraleThreshold)
        {
            animator.SetTrigger("Panic");
            rb.velocity = Vector2.zero;
            return true;
        }
        // Clean up destroyed or missing enemies
        blackboard.detectedEnemies.RemoveAll(enemy => enemy == null || enemy.Equals(null));
        // If no enemies detected, reset to patrol
        if (blackboard.detectedEnemies.Count == 0)
        {
            blackboard.currentTarget = null;
            blackboard.ResetEngageState();
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
            rb.velocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            return true;
        }
        // Check if target is too far, reset to patrol
        float maxChaseDist = blackboard.chaseRadius;
        float distToTarget = Vector2.Distance(transform.position, target.position);
        if (distToTarget > maxChaseDist)
        {
            blackboard.currentTarget = null;
            blackboard.ResetEngageState();
            rb.velocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            return true;
        }
        Vector2 dirToTarget = new Vector2((target.position - transform.position).normalized.x, 0);
        float distToTargetX = Mathf.Abs(target.position.x - transform.position.x); // Only x axis
        // Flip sprite to face target
        if (dirToTarget.x > 0 && !facingRight || dirToTarget.x < 0 && facingRight)
        {
            Flip();
        }
        switch (blackboard.combatType)
        {
            case AllyCombatType.Melee:
                // Move to enemy and attack if close
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
                // Maintain safe distance for ranged
                float safeDist = 6f;
                if (distToTargetX < safeDist)
                {
                    // Keep running away until safe distance is reached
                    float runDir = Mathf.Sign(transform.position.x - target.position.x);
                    rb.velocity = new Vector2(runDir * blackboard.walkSpeed, 0);
                    animator.SetBool("IsMoving", true);
                    // Flip sprite to face away from enemy while running
                    if ((runDir > 0 && !facingRight || runDir < 0 && facingRight) && !animator.GetCurrentAnimatorStateInfo(0).IsName("Archer_attack"))
                    {
                        Flip();
                    }
                }
                else if (distToTargetX <= blackboard.engageRange)
                {
                    // Safe distance reached, stop and attack
                    rb.velocity = Vector2.zero;
                    animator.SetBool("IsMoving", false);
                    // Flip sprite to face enemy before attacking
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
                // Buff or heal allies in range
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
        // Pursue lost target
        if (!blackboard.isPursuing && blackboard.currentTarget != null)
        {
            blackboard.isPursuing = true;
            blackboard.lastKnownEnemyPosition = blackboard.currentTarget.position;
            blackboard.searchTimer = 0f;
        }
        if (blackboard.currentTarget == null)
        {
            blackboard.searchTimer += Time.deltaTime;
            float dirX = Mathf.Sign(blackboard.lastKnownEnemyPosition.x - transform.position.x);
            rb.velocity = new Vector2(dirX * blackboard.walkSpeed, 0);
            animator.SetBool("IsMoving", true);
            Vector2 dir = (blackboard.lastKnownEnemyPosition - (Vector2)transform.position).normalized;
            float dist = Vector2.Distance(transform.position, blackboard.lastKnownEnemyPosition);
            if (dist > 0.5f)
            {
                rb.velocity = dir * blackboard.walkSpeed;
                animator.SetBool("IsMoving", true);
            }
            else
            {
                rb.velocity = Vector2.zero;
                animator.SetBool("IsMoving", false);
            }
            if (blackboard.searchTimer > blackboard.maxSearchTime)
            {
                blackboard.ResetPursueState();
                return true;
            }
            return false;
        }
        float distFromStart = Vector2.Distance(transform.position, blackboard.patrolPoints[0].position);
        if (distFromStart > blackboard.chaseRadius)
        {
            blackboard.ResetPursueState();
            return true;
        }
        float dirX2 = Mathf.Sign(blackboard.currentTarget.position.x - transform.position.x);
        rb.velocity = new Vector2(dirX2 * blackboard.walkSpeed, 0);
        animator.SetBool("IsMoving", true);
        Vector2 dirToTarget = (blackboard.currentTarget.position - transform.position).normalized;
        float distToTarget = Vector2.Distance(transform.position, blackboard.currentTarget.position);
        if (dirToTarget.x > 0 && !facingRight || dirToTarget.x < 0 && facingRight)
        {
            Flip();
        }
        rb.velocity = dirToTarget * blackboard.walkSpeed;
        animator.SetBool("IsMoving", true);
        if (distToTarget <= blackboard.engageRange)
        {
            blackboard.ResetPursueState();
            return true;
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

    public void OnCombatSoundHeard(Vector2 soundPosition)
    {
        // React to combat sound if within hearing range
        if (Vector2.Distance(transform.position, soundPosition) <= blackboard.hearingRange)
        {
            blackboard.HandleCombatSound(soundPosition);
        }
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

    public void ShootArrow2()
    {
        // Shoot arrow in facing direction (no target)
        if (arrowPrefab != null && arrowSpawnPoint != null)
        {
            Vector2 dir = facingRight ? Vector2.right : Vector2.left;
            GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.identity);
            ArrowController arrowCtrl = arrow.GetComponent<ArrowController>();
            if (arrowCtrl != null)
            {
                arrowCtrl.Init(dir, null);
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
