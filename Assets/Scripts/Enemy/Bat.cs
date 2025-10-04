using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class Bat : Enemy
{
    [Header("A Star Algorithms")]
    [SerializeField] private float pathUpdateInterval = 0.5f; // Route update frequency
    [SerializeField] private float nextWaypointDistance = 0f; // Distance to move to next waypoint
    private Path currentPath;
    private Seeker seeker;
    private int currentWaypoint = 0;
    private float lastPathUpdateTime = 0f;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float maxChasingDistance = 10f;
    [SerializeField] private float maxDistanceFromStart = 20f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask allyLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Transform currentTarget;
    [SerializeField] private List<Transform> detectedEnemies = new();
    [SerializeField] private Transform ground;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float lastTimeAttack = 0f;

    // A* Pathfinding components
    private Vector3 originalPos;
    private float minHeight = 2f;

    
    protected override void Awake()
    {
        base.Awake();
        StartCoroutine(DetectionUpdate());
    }

    protected override void Start()
    {
        base.Start();

        seeker = GetComponent<Seeker>();
        originalPos = transform.position;
        ChangeState(EnemyStates.IDLE);

        // Automatically find the ground object by name
        GameObject groundObject = GameObject.Find("BaseGround");
        if (groundObject != null)
        {
            ground = groundObject.transform;
        }
        else
        {
            Debug.LogWarning("Ground object not found. Please ensure there is an object named 'ground' in the scene.");
        }
    }

    protected void FixedUpdate()
    {
        base.Update();

        // Ensure the Bat stays above the minimum height
        if (transform.position.y < ground.position.y + minHeight)
        {
            Vector2 targetPosition = new Vector2(transform.position.x, ground.position.y + minHeight);
            StartCoroutine(MoveToPosition(targetPosition, null));
        }
    }
    protected override void UpdateEnemyState()
    {
        switch (GetCurrentEnemyState)
        {              
            case EnemyStates.CHASE:
                Chase();
                break;
            case EnemyStates.RETURN_TO_START:
                ReturnToStart();
                break;
            case EnemyStates.STUNNED:
                Stunned();
                break;
            case EnemyStates.DEATH:
                Death();
                break;
            case EnemyStates.ATTACK:
                Attack();
                break;
            default:
                Idle();
                break;
        }
    }

    private System.Collections.IEnumerator DetectionUpdate()
    {
        while (true)
        {
            UpdateDetection();
            yield return new WaitForSeconds(0.1f); // Update 10 times per second
        }
    }
    private void UpdateDetection()
    {
        // Detect players
        Collider2D[] players = Physics2D.OverlapCircleAll(transform.position,
            detectionRange, playerLayer);

        // Detect allies
        Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position,
            detectionRange, allyLayer);

        // Combine detected players and allies
        List<Transform> detectedTargets = new List<Transform>();

        foreach (var player in players)
        {
            if ((playerLayer & (1 << player.gameObject.layer)) != 0)
            {
                detectedTargets.Add(player.transform);
            }
        }
        foreach (var ally in allies)
        {
            if ((allyLayer & (1 << ally.gameObject.layer)) != 0)
            {
                detectedTargets.Add(ally.transform);
            }
        }
        detectedEnemies = detectedTargets;
        // Set the first detected target as the current target
        if (detectedTargets.Count > 0)
        {
            currentTarget = detectedTargets[0];
        }
    }

    #region Process create path and make the bat follow the path
    void CreatePathToPosition(Vector3 targetPosition)
    {
        if (seeker.IsDone())
        {
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);
            lastPathUpdateTime = Time.time;
        }
    }
    void CreateChasePath()
    {
        if (seeker.IsDone())
        {
            Vector3 targetPosition = new Vector3 (currentTarget.position.x, currentTarget.position.y + 1f, 0);
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);
            lastPathUpdateTime = Time.time;
        }
    }
    void CreateReturnPath()
    {
        if (seeker.IsDone())
        {
            seeker.StartPath(transform.position, originalPos, OnPathComplete);
            lastPathUpdateTime = Time.time;
        }
    }
    void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            currentPath = p;
            currentWaypoint = 0;
        }
    }
    void FollowPath(float speedMultiplier = 1f)
    {
        if (currentPath == null || currentWaypoint >= currentPath.vectorPath.Count)
        {
            return;
        }
        float currentSpeed = speed * speedMultiplier;
        Vector2 direction = ((Vector2)currentPath.vectorPath[currentWaypoint] - (Vector2)transform.position).normalized;

        Vector2 newPosition = (Vector2)transform.position + direction * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        float distanceToWaypoint = Vector2.Distance(transform.position, currentPath.vectorPath[currentWaypoint]);
        if (distanceToWaypoint < nextWaypointDistance)
        {
            currentWaypoint++;
        }

        FlipBat(direction.x);
    }
    void AlterFollowPath(float speedMultiplier = 1f)
    {
        if (currentPath == null || currentWaypoint >= currentPath.vectorPath.Count)
        {
            return;
        }

        float currentSpeed = speed * speedMultiplier;

        // Calculate direction to the next waypoint
        Vector2 targetPosition = currentPath.vectorPath[currentWaypoint];

        Vector2 direction = targetPosition - (Vector2)transform.position;

        // Use MoveTowards to ensure constant speed
        rb.AddForce(direction * currentSpeed, ForceMode2D.Force);

        float distanceToWaypoint = Vector2.Distance(transform.position, targetPosition);
        if (distanceToWaypoint < nextWaypointDistance)
        {
            currentWaypoint++;
        }

        // Auto flip based on movement direction
        FlipBat(direction.x);
    }
    #endregion
    void Idle()
    {
        if (currentTarget == null)
        {
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distanceToTarget < detectionRange)
        {
            ChangeState(EnemyStates.CHASE);
            CreateChasePath();
        }
    }
    void Chase()
    {
        if (currentPath == null)
            return;

        float distanceFromStart = Vector2.Distance(transform.position, originalPos);
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Transition to ATTACK state if the target is within attack range
        if (distanceToTarget <= attackRange)
        {
            ChangeState(EnemyStates.ATTACK);
            return;
        }

        // Stop chasing if the target is too far or the Bat is too far from its starting position
        if (distanceFromStart > maxDistanceFromStart || distanceToTarget > maxChasingDistance)
        {
            currentPath = null;
            ChangeState(EnemyStates.RETURN_TO_START);
            return;
        }

        float chaseMultiSpeed = 1.5f;

        if (Time.time > lastPathUpdateTime + pathUpdateInterval)
        {
            CreateChasePath();
        }
        FollowPath(chaseMultiSpeed);
    }
    void ReturnToStart()
    {
        float returnMultiSpeed = 1f;
        float distanceToStart = Vector2.Distance(transform.position, originalPos);
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        if (Mathf.Abs(distanceToStart - maxDistanceFromStart) <= detectionRange)
        {
            if (distanceToTarget < detectionRange)
            {
                currentPath = null;
                ChangeState(EnemyStates.CHASE);
                CreateChasePath();
                return;
            }
        }

        if (Time.time > lastPathUpdateTime + pathUpdateInterval)
        {
            CreateReturnPath();
        }
        if (distanceToStart <= 1f)
        {
            currentPath = null;
            transform.position = originalPos;
            rb.velocity = Vector2.zero;
            ChangeState(EnemyStates.IDLE);
            return;
        }

        FollowPath(returnMultiSpeed);
    }
    void Stunned()
    {
        float distanceFromStart = Vector2.Distance(transform.position, originalPos);
        float distanceToPlayer = Vector2.Distance(transform.position, currentTarget.position);
        
        if (distanceFromStart > maxDistanceFromStart)
        {
            ChangeState(EnemyStates.RETURN_TO_START);
        }
        else
        {
            ChangeState(EnemyStates.IDLE);
        }
        rb.velocity = Vector2.zero;        
    }
    void Death()
    {
        rb.gravityScale = 12f;
        Destroy(gameObject, 1f);
    }
    protected override void Attack()
    {
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (currentTarget == null || distanceToTarget > detectionRange)
        {
            // No target, return to original position
            ChangeState(EnemyStates.CHASE);
            return;
        }

        // Ensure the Bat is facing the target
        FlipBat(currentTarget.position.x - transform.position.x);

        if (distanceToTarget <= attackRange && Time.time > lastTimeAttack + attackCooldown)
        {
            // Perform a diagonal lunge attack
            lastTimeAttack = Time.time;

            // Calculate the diagonal starting position (45 degrees above the target)
            Vector2 targetPosition = currentTarget.position + new Vector3(0, 1.3f, 0); // Adjust target position
            Vector2 diagonalStartPosition = targetPosition + new Vector2(
                Mathf.Sign(transform.position.x - targetPosition.x) * 1.3f, 2.5f);

            // Move the Bat to the diagonal starting position
            StartCoroutine(MoveToPosition(diagonalStartPosition, () => StartCoroutine(DiagonalLungeAttack(targetPosition))));
        }
    }
    private IEnumerator MoveToPosition(Vector2 targetPosition, System.Action onComplete)
    {
        while (Vector2.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        // Call the onComplete action once the Bat reaches the position
        onComplete?.Invoke();
    }

    private IEnumerator DiagonalLungeAttack(Vector2 targetPosition)
    {
        float lungeForce =10f; 
        float retreatForce = 10f; 
        float lungeDuration = 0.3f; 
        float retreatDuration = 0.3f; 

        // Lunge towards the target
        Vector2 lungeDirection = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
        rb.AddForce(lungeDirection * lungeForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(lungeDuration);
        rb.velocity = Vector2.zero;
        
        // Retreat back to the original 
        Vector2 retreatDirection = (-1) * lungeDirection;
        rb.AddForce(retreatDirection * retreatForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(retreatDuration);
        rb.velocity = Vector2.zero;

        // Return to CHASE state after the attack
        ChangeState(EnemyStates.CHASE);
    }
    public override void EnemyHit(float damage, Vector2 hitDirection, float hitForce)
    {
        base.EnemyHit(damage, hitDirection, hitForce);
        if (health <= 0)
        {
            ChangeState(EnemyStates.DEATH);
        }
        else
        {
            ChangeState(EnemyStates.STUNNED);
        }
    }
    protected override void ChangeCurrentAnimation()
    {
        anim.SetBool("IsMoving", GetCurrentEnemyState == EnemyStates.CHASE || GetCurrentEnemyState == EnemyStates.RETURN_TO_START);
        if (GetCurrentEnemyState == EnemyStates.STUNNED)
        {
            anim.SetTrigger("Stunned");
        }
        if (GetCurrentEnemyState == EnemyStates.DEATH && !anim.GetCurrentAnimatorStateInfo(0).IsName("Death"))
        {
            anim.SetTrigger("Death");
        }
    }

    //Adjust FlipBat to receive new direction
    void FlipBat(float directionX = 0)
    {
        sr.flipX = directionX < 0;
    }

    private void OnDrawGizmosSelected()
    {
        //draw chase range in Scene view
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(originalPos, maxDistanceFromStart);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxChasingDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // draw path
        if (currentPath != null)
        {
            Gizmos.color = Color.blue;
            for (int i = currentWaypoint; i < currentPath.vectorPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath.vectorPath[i], currentPath.vectorPath[i + 1]);
            }
        }
    }

    
}