using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BatBlackboard : EnemyBlackboard
{
    [Header("Movement Settings")]
    public float maxDistanceFromStart = 20f;
    public float maxChasingDistance = 10f;
    public Vector3 startPosition;

    [Header("A* Pathfinding")]
    public float pathUpdateInterval = 0.5f;
    public float nextWaypointDistance = 3f;

    [Header("A* Path Data")]
    public Pathfinding.Path currentPath;
    public int currentWaypoint = 0;
    public float lastPathUpdateTime = 0f;

    // Helper methods
    public new bool IsHealthBelowPercent(float percent)
    {
        return hp < maxHp * percent;
    }

    public bool HasTarget()
    {
        return currentTarget != null;
    }

    public float GetDistanceToTarget()
    {
        if (currentTarget == null) return float.MaxValue;
        return Vector2.Distance(transform.position, currentTarget.position);
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
        hp = maxHp;
    }
}
