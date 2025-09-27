using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using BehaviorTree;

[RequireComponent(typeof(BatController))]
public class Bat : Enemy
{
    [Header("Bat Components")]
    public BatController batController;

    [Header("A Star Algorithms")]
    [SerializeField] private float pathUpdateInterval = 0.5f; // Route update frequency
    [SerializeField] private float nextWaypointDistance = 3f; // Distance to move to next waypoint

    [Header("Chase Settings")]
    [SerializeField] private float detectedPlayerRange = 8f;
    [SerializeField] private float maxChasingDistance = 10f;
    [SerializeField] private float maxDistanceFromStart = 20f;

    [Header("Stunned Settings")]
    [SerializeField] private float stunDuration = 1f;
    private float timer;

    // A* Pathfinding components
    private Path currentPath;
    private Seeker seeker;
    private int currentWaypoint = 0;
    //private bool reachedEndOfPath = false;
    private bool isReturningToStart = false;

    private Vector3 startPosition;
    private float lastPathUpdateTime = 0f;

    protected override void Awake()
    {
        base.Awake();

        // Initialize bat controller if not assigned
        if (batController == null)
        {
            batController = GetComponent<BatController>();
            if (batController == null)
            {
                batController = gameObject.AddComponent<BatController>();
            }
        }
    }

    protected override void Start()
    {
        base.Start();

        // Set bat specific stats
        health = 50f;
        speed = 3f;
        damage = 1f;

        // Initialize bat controller blackboard with current health
        if (batController != null && batController.blackboard != null)
        {
            batController.blackboard.currentHP = health;
            batController.blackboard.maxHP = health;
            batController.blackboard.moveSpeed = speed;
        }

        seeker = GetComponent<Seeker>();
        startPosition = transform.position;
        ChangeState(EnemyStates.Bat_Idle);
    }

    protected override void Update()
    {
        // Sync health between Enemy base class and Bat blackboard
        if (batController != null && batController.blackboard != null)
        {
            batController.blackboard.currentHP = health;

            // Sync death state
            if (health <= 0 && !batController.blackboard.isDead)
            {
                batController.blackboard.isDead = true;
            }
        }

        // Note: BatController handles all behavior through its own Update method
        // No need to call base.Update() as we're using Behavior Tree system now
    }

    public override void EnemyHit(float damage, Vector2 hitDirection, float hitForce)
    {
        // Apply damage using base Enemy logic
        base.EnemyHit(damage, hitDirection, hitForce);

        // Forward damage to bat controller for behavior tree logic
        if (batController != null)
        {
            batController.TakeDamage(damage, hitDirection, hitForce);
        }
    }

    protected override void ChangeCurrentAnimation()
    {
        // Animation is handled by BatController now
        // This method is kept for compatibility but does nothing
    }

    // Keep gizmos for debugging - delegate to controller
    private void OnDrawGizmosSelected()
    {
        if (batController != null)
        {
            // BatController handles gizmo drawing
            return;
        }

        // Fallback gizmo if controller not available
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 8f); // Default detection range
    }
}
