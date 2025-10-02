using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

public class Skeleton : Enemy
{
    private Vector2 originalPos;
    private bool isDeath = false;

    [Header("Chase Settings")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float maxChasingDistance = 12f;
    [SerializeField] private float maxDistanceFromStart = 20f;
    [SerializeField] private float chasingSpeed;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRange = 5f;
    [SerializeField] private float ledgeCheckX = 1f;
    [SerializeField] private float ledgeCheckY = 2f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 2f;
    private float lastAttackTime;

    private Transform currentTarget;
    private enum SkeletonExtraState { CounterAttack = 1000 }

    protected override void Awake()
    {
        base.Awake();

        originalPos = transform.position;
        chasingSpeed = speed * 2f;
        currentEnemyState = EnemyStates.PATROL;
    }

    protected override void Update()
    {
        base.Update();
    }
    protected override void UpdateEnemyState()
    {
        base.UpdateEnemyState();
        switch (GetCurrentEnemyState)
        {
            case EnemyStates.IDLE:
                Idle();
                break;
            case EnemyStates.CHASE:
                Chase();
                break;
            case EnemyStates.PATROL:
                Patrol();
                break;
            case EnemyStates.ATTACK:
                PerformAttack();
                break;
            case EnemyStates.RETURN_TO_START:
                ReturnToStartPosition();
                break;
            case EnemyStates.STUNNED:
                Stunned();
                break;
            case EnemyStates.DEATH:
                Death();
                break;
            case (EnemyStates)SkeletonExtraState.CounterAttack:
                CounterAttack();
                break;
        }
    }
    void Idle()
    {
        // Idle is now a fallback/rest state, not a decision hub
        anim.SetBool("Walk", false);
        rb.velocity = Vector2.zero;
        
        // Simple detection with immediate action (for cases when called from other states)
        Transform nearestTarget = FindNearestEnemyTarget();
        if (nearestTarget != null)
        {
            currentTarget = nearestTarget;
            float dist = Vector2.Distance(transform.position, nearestTarget.position);
            
            if (Time.time >= (lastAttackTime + attackCooldown) && dist <= attackRange)
            {
                ChangeState(EnemyStates.ATTACK);
            }
            else if (dist > attackRange)
            {
                ChangeState(EnemyStates.CHASE);
            }
        }
        else
        {
            // No target found, return to patrol after short idle time
            float distanceToStart = Vector2.Distance(transform.position, originalPos);
            if (distanceToStart > 1f)
            {
                ChangeState(EnemyStates.RETURN_TO_START);
            }
            else
            {
                ChangeState(EnemyStates.PATROL);
            }
        }
    }
    void Patrol()
    {
        // Detection and state transition logic (execute first)
        Transform nearestTarget = FindNearestEnemyTarget();
        if (nearestTarget != null)
        {
            currentTarget = nearestTarget;
            float dist = Vector2.Distance(transform.position, nearestTarget.position);
            
            // Direct transition based on distance
            if (Time.time >= (lastAttackTime + attackCooldown) && dist <= attackRange)
            {
                // Can attack immediately
                anim.SetBool("Walk", false);
                ChangeState(EnemyStates.ATTACK);
                return;
            }
            else if (dist > attackRange && dist <= detectionRange)
            {
                // Need to chase first
                ChangeState(EnemyStates.CHASE);
                return;
            }
        }
        
        // Patrol movement logic (execute only if no target found)
        Vector3 ledgeCheckStart = transform.localScale.x > 0 ? new Vector3(-ledgeCheckX, 1.5f) : new Vector3(ledgeCheckX, 1.5f);
        Vector2 wallCheckDir = transform.localScale.x > 0 ? -transform.right : transform.right;
        
        // Edge and wall detection
        if (!Physics2D.Raycast(transform.position + ledgeCheckStart, Vector2.down, ledgeCheckY, whatIsGround)
            || Physics2D.Raycast(transform.position + new Vector3(0, 0.5f, 0), wallCheckDir, ledgeCheckX, whatIsGround))
        {
            Flip();
        }
        
        // Continue patrol animation and movement
        anim.SetBool("Walk", true);
        
        float currentPatrolPosition = transform.position.x;
        float leftBound = originalPos.x - patrolRange;
        float rightBound = originalPos.x + patrolRange;

        if (isFacingRight)
        {
            transform.Translate(Vector2.right * speed * Time.deltaTime);
            if (currentPatrolPosition >= rightBound)
            {
                Flip();
            }
        }
        else
        {
            transform.Translate(Vector2.left * speed * Time.deltaTime);
            if (currentPatrolPosition <= leftBound)
            {
                Flip();
            }
        }
    }
    
    private Transform FindNearestEnemyTarget()
    {
        Transform nearest = null;
        float minDist = float.MaxValue;
        // Use OverlapCircleAll to get all colliders in detection range on Player and Ally layers
        int mask = LayerMask.GetMask("Player", "Ally");
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, mask);
        foreach (var hit in hits)
        {
            if (hit.transform == this.transform) continue;
            if (!hit.CompareTag("Player") && !hit.CompareTag("Ally")) continue;
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            Vector2 dir = (hit.transform.position - transform.position).normalized;
            RaycastHit2D ray = Physics2D.Raycast(transform.position, dir, dist, whatIsGround | LayerMask.GetMask(hit.tag));
            if (ray.collider != null && ray.collider.transform == hit.transform)
            {
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = hit.transform;
                }
            }
        }
        return nearest;
    }
    void Chase()
    {
        anim.SetBool("Walk", true);
        if (currentTarget == null) currentTarget = player.transform;
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        float distanceToStartPos = Vector2.Distance(transform.position, originalPos);

        if (distanceToTarget > maxChasingDistance || distanceToStartPos > maxDistanceFromStart)
        {
            ChangeState(EnemyStates.RETURN_TO_START);
            return;
        }
        if (distanceToTarget <= attackRange)
        {
            anim.SetBool("Walk", false);
            currentEnemyState = EnemyStates.ATTACK;
            return;
        }
        Vector2 direction = (currentTarget.position - transform.position).normalized;
        float attackDir = isFacingRight ? 1f : -1f;
        transform.position = Vector2.MoveTowards(transform.position, new Vector2(currentTarget.position.x + attackRange * attackDir, transform.position.y), chasingSpeed * Time.deltaTime);
        float deltaX = currentTarget.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) > 1f) 
        {
            if (deltaX > 0 && !isFacingRight) Flip();
            else if (deltaX < 0 && isFacingRight) Flip();
        }
    }
    void ReturnToStartPosition()
    {
        float distanceToStart = Vector2.Distance(transform.position, originalPos);
        if (distanceToStart <= 0.1f)
        {
            transform.position = originalPos;
            anim.SetBool("Walk", false);
            currentEnemyState = EnemyStates.PATROL;
            return;
        }
        Vector2 direction = (originalPos - (Vector2)transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, originalPos, chasingSpeed * Time.deltaTime);
        if (direction.x > 0 && !isFacingRight)
            Flip();
        else if (direction.x < 0 && isFacingRight)
            Flip();
    }
    void PerformAttack()
    {
        if (currentTarget == null)
        {
            // No target, return to patrol
            float distanceToStart = Vector2.Distance(transform.position, originalPos);
            if (distanceToStart > 1f)
            {
                ChangeState(EnemyStates.RETURN_TO_START);
            }
            else
            {
                ChangeState(EnemyStates.PATROL);
            }
            return;
        }
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Check if target is out of range
        if (distanceToTarget > attackRange && !anim.GetCurrentAnimatorStateInfo(0).IsName("Ske_Attack"))
        {
            currentEnemyState = EnemyStates.CHASE;
            return;
        }

        rb.velocity = Vector2.zero;
        // Check if the target is in front
        Vector2 directionToTarget = (currentTarget.position - transform.position).normalized;
        float deltaX = currentTarget.position.x - transform.position.x;
        bool targetInFront = (isFacingRight && directionToTarget.x > 0) || (!isFacingRight && directionToTarget.x < 0);

        if (!targetInFront && Mathf.Abs(deltaX) > 0.1f)
        {
            Flip();
            return;
        }
        // If target is dead or inactive, return to patrol
        if (!currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
            float distanceToStart = Vector2.Distance(transform.position, originalPos);
            if (distanceToStart > 1f)
            {
                ChangeState(EnemyStates.RETURN_TO_START);
            }
            else
            {
                ChangeState(EnemyStates.PATROL);
            }
            return;
        }
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            anim.SetTrigger("Attack");
            lastAttackTime = Time.time;
        }
    }
    protected override void Attack()
    {
        PerformAttackTarget(currentTarget);
    }
    void Death()
    {
        if (!isDeath)
        {
            isDeath = true;
            rb.velocity = Vector2.zero;
            Destroy(gameObject, 1f);
        }
    }
    void Stunned()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Ske_Hurt"))
        {
            rb.velocity = Vector2.zero;
            return;
        }
        if (currentTarget != null)
        {
            ChangeState((EnemyStates)SkeletonExtraState.CounterAttack);
            return;
        }

        float distanceToStart = Vector2.Distance(transform.position, originalPos);
        if (distanceToStart > 1f)
        {
            ChangeState(EnemyStates.RETURN_TO_START);
        }
        else
        {
            ChangeState(EnemyStates.PATROL);
        }
    }
    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    public override void EnemyHit(float damage, Vector2 hitDirection, float hitForce)
    {
        
        base.EnemyHit(damage, hitDirection, hitForce);
        if (health <= 0)
        {
            anim.SetTrigger("Death");
            ChangeState(EnemyStates.DEATH);
        }
        else
        {
            anim.SetTrigger("Stunned");
            currentTarget = FindNearestEnemyTarget();
            ChangeState(EnemyStates.STUNNED);
        }
    }
    private void CounterAttack()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = FindNearestEnemyTarget();
            if (currentTarget == null)
            {
                float distToOrigin = Vector2.Distance(transform.position, originalPos);
                if (distToOrigin > 1f)
                {
                    ChangeState(EnemyStates.RETURN_TO_START);
                }
                else
                {
                    ChangeState(EnemyStates.PATROL);
                }
                return;
            }
        }
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distanceToTarget > attackRange)
        {
            ChangeState(EnemyStates.CHASE);
        }
        else
        {
            ChangeState(EnemyStates.ATTACK);
        }
    }
    private void OnDrawGizmos()
    {
        Vector3 ledgeCheckStart = transform.localScale.x > 0 ? new Vector3(-ledgeCheckX, 1.5f) : new Vector3(ledgeCheckX, 1.5f);
        Vector2 wallCheckDir = transform.localScale.x > 0 ? -transform.right : transform.right;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + ledgeCheckStart, Vector3.down * ledgeCheckY);
        Gizmos.DrawRay(transform.position + new Vector3(0, 0.5f, 0), wallCheckDir);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Water"))
        {
            anim.SetTrigger("Death");
            ChangeState(EnemyStates.DEATH);
        }
    }
}
