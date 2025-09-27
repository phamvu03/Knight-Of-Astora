using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BatBlackboard
{
    [Header("Detection Settings")]
    public float detectionRange = 8f;
    public LayerMask playerLayer = -1; // Layer for Player detection
    public LayerMask obstacleLayer = -1; // Layer for obstacles
    
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float chaseSpeedMultiplier = 2f;
    public float returnSpeedMultiplier = 2.5f;
    public float maxDistanceFromStart = 20f;
    public float maxChasingDistance = 10f;
    
    [Header("A* Pathfinding")]
    public float pathUpdateInterval = 0.5f;
    public float nextWaypointDistance = 3f;
    
    [Header("Health")]
    public float currentHP = 50f;
    public float maxHP = 50f;
    
    [Header("Runtime Data")]
    public Transform targetPlayer;
    public List<Transform> detectedPlayers = new List<Transform>();
    public Vector3 startPosition;
    public bool isChasing = false;
    public bool isReturning = false;
    public bool isDead = false;
    
    [Header("A* Path Data")]
    public Pathfinding.Path currentPath;
    public int currentWaypoint = 0;
    public float lastPathUpdateTime = 0f;
    
    // Helper methods
    public bool IsHealthBelowPercent(float percent)
    {
        return currentHP < maxHP * percent;
    }
    
    public bool HasTarget()
    {
        return targetPlayer != null;
    }
    
    public float GetDistanceToTarget()
    {
        if (targetPlayer == null) return float.MaxValue;
        return Vector2.Distance(transform.position, targetPlayer.position);
    }
    
    public float GetDistanceToStart(Vector3 currentPosition)
    {
        return Vector2.Distance(currentPosition, startPosition);
    }
    
    // Reference to transform for distance calculations
    private Transform transform;
    
    public void Initialize(Transform batTransform)
    {
        transform = batTransform;
        startPosition = batTransform.position;
        currentHP = maxHP;
    }
}