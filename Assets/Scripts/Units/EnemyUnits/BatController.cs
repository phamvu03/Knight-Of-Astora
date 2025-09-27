using UnityEngine;
using BehaviorTree;
using Pathfinding;

[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class BatController : MonoBehaviour
{
    [Header("Bat Settings")]
    public BatBlackboard blackboard;
    
    [Header("Layer Masks")]
    public LayerMask playerLayer = -1;
    public LayerMask obstacleLayer = -1;
    
    private BatBT _bt;
    private Seeker _seeker;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _animator;
    private bool _facingRight = true;

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
        blackboard.playerLayer = playerLayer;
        blackboard.obstacleLayer = obstacleLayer;
        
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
    }

    // Detection system using OverlapCircle instead of singleton
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
        // Clear previous detections
        blackboard.detectedPlayers.Clear();
        blackboard.targetPlayer = null;
        
        // Find players using OverlapCircle
        Collider2D[] players = Physics2D.OverlapCircleAll(transform.position, blackboard.detectionRange, blackboard.playerLayer);
        
        Transform closestPlayer = null;
        float closestDistance = float.MaxValue;
        
        foreach (var playerCollider in players)
        {
            if (playerCollider.CompareTag("Player"))
            {
                blackboard.detectedPlayers.Add(playerCollider.transform);
                
                // Find closest player
                float distance = Vector2.Distance(transform.position, playerCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = playerCollider.transform;
                }
            }
        }
        
        // Set target to closest player
        if (closestPlayer != null)
        {
            blackboard.targetPlayer = closestPlayer;
        }
    }

    // Action Methods for Behavior Tree
    
    public BehaviorState IdleAction()
    {
        _rb.velocity = Vector2.zero;
        blackboard.isChasing = false;
        blackboard.isReturning = false;
        
        return BehaviorState.Success;
    }

    public BehaviorState ChaseAction()
    {
        if (blackboard.targetPlayer == null || blackboard.currentPath == null)
            return BehaviorState.Failure;
            
        blackboard.isChasing = true;
        blackboard.isReturning = false;
        
        // Update path if needed
        if (Time.time > blackboard.lastPathUpdateTime + blackboard.pathUpdateInterval)
        {
            CreateChasePath();
        }
        
        // Follow path
        FollowPath(blackboard.chaseSpeedMultiplier);
        
        return BehaviorState.Running;
    }

    public BehaviorState ReturnToStartAction()
    {
        blackboard.isReturning = true;
        blackboard.isChasing = false;
        
        // Update path if needed
        if (Time.time > blackboard.lastPathUpdateTime + blackboard.pathUpdateInterval)
        {
            CreateReturnPath();
        }
        
        // Follow path
        FollowPath(blackboard.returnSpeedMultiplier);
        
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
        blackboard.isDead = true;
        Destroy(gameObject, 1f);
        return BehaviorState.Success;
    }

    // Path creation methods
    private void CreateChasePath()
    {
        if (_seeker.IsDone() && blackboard.targetPlayer != null)
        {
            Vector3 targetPosition = blackboard.targetPlayer.position + new Vector3(0, 1f, 0);
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
        
        _animator.SetBool("Chase", blackboard.isChasing || blackboard.isReturning);
        
        if (blackboard.isDead)
        {
            _animator.SetTrigger("Death");
        }
    }

    public void TakeDamage(float damage, Vector2 hitDirection, float hitForce)
    {
        blackboard.currentHP -= damage;
        
        if (blackboard.currentHP <= 0)
        {
            blackboard.isDead = true;
        }
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (blackboard == null) return;
        
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, blackboard.detectionRange);
        
        // Chase range from start
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(blackboard.startPosition, blackboard.maxDistanceFromStart);
        
        // Max chasing distance
        Gizmos.color = Color.red;
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
    }
}