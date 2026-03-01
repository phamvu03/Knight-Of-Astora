using UnityEngine;
using BehaviorTree;
using Pathfinding;
using Unity.Entities;
using System.Collections.Generic; // Added for List<T>

[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class BatController : MonoBehaviour
{
    [Header("Bat Settings")]
    public BatBlackboard blackboard;

    private BatBT _bt;
    private Seeker _seeker;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _animator;
    private bool _facingRight = true;

    // Dynamic states
    private bool isChasing;
    private bool isReturning;
    private bool isDead;

    private void Awake()
    {
        // Initialize components
        _seeker = GetComponent<Seeker>();
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        // Initialize blackboard
        if (blackboard == null)
            blackboard = new BatBlackboard();

        blackboard.Initialize(transform);

        // Start detection coroutine
        StartCoroutine(DetectionUpdate());
    }

    private void Start()
    {
        _bt = new BatBT(blackboard, this);
    }

    private void Update()
    {
        // Update blackboard state
        UpdateBlackboard();

        // Tick behavior tree
        _bt.Tick();

        // Update animations
        UpdateAnimations();
    }

    private void UpdateBlackboard()
    {
        // Update path update timer
        blackboard.lastPathUpdateTime += Time.deltaTime;

        // Update death state based on inherited hp
        if (blackboard.hp <= 0 && !isDead)
        {
            isDead = true;
        }
    }

    // Detection system using OverlapCircle
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
            blackboard.detectionRange, blackboard.playerLayer);

        // Detect allies
        Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, 
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

        foreach (var ally in allies)
        {
            if ((blackboard.allyLayer & (1 << ally.gameObject.layer)) != 0)
            {
                detectedTargets.Add(ally.transform);
            }
        }

        // Update blackboard with detected targets
        blackboard.detectedEnemies = detectedTargets;

        // Set the first detected target as the current target
        if (detectedTargets.Count > 0)
        {
            blackboard.currentTarget = detectedTargets[0];
        }
        else
        {
            blackboard.currentTarget = null;
        }
    }

    // Condition Methods for Behavior Tree
    public bool IsDead()
    {
        return isDead;
    }

    public bool ShouldReturnToStart()
    {
        return isReturning;
    }

    public bool ShouldChase()
    {
        return isChasing;
    }

    public bool ShouldIdle()
    {
        return !isChasing && !isReturning && !isDead;
    }

    // Action Methods for Behavior Tree

    public BehaviorState IdleAction()
    {
        _rb.linearVelocity = Vector2.zero;
        isChasing = false;
        isReturning = false;

        return BehaviorState.Success;
    }

    public BehaviorState ChaseAction()
    {
        if (blackboard.currentTarget == null)
            return BehaviorState.Failure;

        isChasing = true;
        isReturning = false;

        // Update path if needed
        if (Time.time > blackboard.lastPathUpdateTime + blackboard.pathUpdateInterval)
        {
            CreateChasePath();
        }
        // Follow path
        FollowPath(blackboard.chaseSpeed);

        return BehaviorState.Running;
    }

    public BehaviorState ReturnToStartAction()
    {
        isReturning = true;
        isChasing = false;

        // Update path if needed
        if (Time.time > blackboard.lastPathUpdateTime + blackboard.pathUpdateInterval)
        {
            CreateReturnPath();
        }

        // Follow path with return speed
        FollowPath(blackboard.retreatSpeed);

        // Check if reached start position
        float distanceToStart = blackboard.GetDistanceToStart(transform.position);
        if (distanceToStart <= 1f)
        {
            transform.position = blackboard.startPosition;
            blackboard.currentPath = null;
            return BehaviorState.Success;
        }

        return BehaviorState.Running;
    }

    public BehaviorState DeathAction()
    {
        _rb.gravityScale = 12f;
        isDead = true;

        Destroy(gameObject, 1f);
        return BehaviorState.Success;
    }

    // Path creation methods
    private void CreateChasePath()
    {
        if (_seeker.IsDone() && blackboard.currentTarget != null)
        {
            Vector3 targetPosition = blackboard.currentTarget.position + new Vector3(0, 1f, 0);
            _seeker.StartPath(transform.position, targetPosition, OnPathComplete);
            blackboard.lastPathUpdateTime = Time.time;
        }
    }

    private void CreateReturnPath()
    {
        if (_seeker.IsDone())
        {
            _seeker.StartPath(transform.position, blackboard.startPosition, OnPathComplete);
            blackboard.lastPathUpdateTime = Time.time;
        }
    }

    private void OnPathComplete(Path path)
    {
        if (!path.error)
        {
            blackboard.currentPath = path;
            blackboard.currentWaypoint = 0;
        }
    }

    private void FollowPath(float speedMultiplier = 1f)
    {
        if (blackboard.currentPath == null || blackboard.currentWaypoint >= blackboard.currentPath.vectorPath.Count)
        {
            return;
        }

        float currentSpeed = blackboard.moveSpeed * speedMultiplier;

        Vector2 direction = ((Vector2)blackboard.currentPath.vectorPath[blackboard.currentWaypoint] - (Vector2)transform.position).normalized;
        Vector2 newPosition = (Vector2)transform.position + direction * currentSpeed * Time.deltaTime;
        _rb.MovePosition(newPosition);

        float distanceToWaypoint = Vector2.Distance(transform.position, blackboard.currentPath.vectorPath[blackboard.currentWaypoint]);
        if (distanceToWaypoint < blackboard.nextWaypointDistance)
        {
            blackboard.currentWaypoint++;
        }

        // Auto flip based on movement direction
        Flip(direction.x);
    }

    private void Flip(float directionX)
    {
        if (directionX < 0 && _facingRight)
        {
            _facingRight = false;
            _sr.flipX = true;
        }
        else if (directionX > 0 && !_facingRight)
        {
            _facingRight = true;
            _sr.flipX = false;
        }
    }

    private void UpdateAnimations()
    {
        if (_animator == null) return;

        // Use more descriptive animation states
        _animator.SetBool("IsMoving", isChasing || isReturning);
    }

    public void TakeDamage(float damage, Vector2 hitDirection, float hitForce)
    {
        blackboard.hp -= (int)damage;
        _animator.SetTrigger("Hurt");
        if (blackboard.hp <= 0 && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Bat_Death"))
        { 
            _animator.SetTrigger("Death");
        }

        // Optional: Add knockback effect using hitDirection and hitForce
        if (_rb != null && hitForce > 0)
        {
            _rb.AddForce(hitDirection.normalized * hitForce, ForceMode2D.Impulse);
        }
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (blackboard == null) return;

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, blackboard.detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blackboard.attackRange);

        // Chase range from start
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(blackboard.startPosition, blackboard.maxDistanceFromStart);

        // Max chasing distance
        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
        Gizmos.DrawWireSphere(transform.position, blackboard.maxChasingDistance);

        // Draw current path
        if (blackboard.currentPath != null)
        {
            Gizmos.color = Color.blue;
            for (int i = blackboard.currentWaypoint; i < blackboard.currentPath.vectorPath.Count - 1; i++)
            {
                Gizmos.DrawLine(blackboard.currentPath.vectorPath[i], blackboard.currentPath.vectorPath[i + 1]);
            }
        }

        // Draw line to target
        if (blackboard.currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, blackboard.currentTarget.position);
        }
    }
}