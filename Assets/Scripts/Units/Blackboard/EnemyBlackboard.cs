using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyBlackboard
{
    [Header("Detect enemy")]
    public Transform currentTarget;
    public List<Transform> detectedEnemies = new();

    [Header("Health")]
    public float hp;
    public float maxHp;
    public float hpLowThreshold;

    [Header("Attack")]
    public bool isAttacking;
    public float attackRange;
    public int attackDamage;
    public float attackCooldown;
    private float lastAttackTime;

    [Header("Detection Settings")]
    public float detectionRange;
    public LayerMask playerLayer;
    public LayerMask allyLayer;
    public LayerMask obstacleLayer; 

    [Header("Movement")]
    public bool isScouting;
    public float moveSpeed;
    public float chaseSpeed;
    public float retreatSpeed;

    [Header("Spawn and Healing")]
    public bool isAtSpawn;
    public float healAtSpawnRate; // HP per second when at spawn
    public Vector3 spawnPosition;

    [Header("Frenzy Mode")]
    public bool isFrenzyActive;
    public float frenzyStartTime;
    public float frenzyDuration = 30f;
    public float cooldownReductionFactor = 0.5f; // Reduces all cooldowns by 50% in frenzy

    [Header("State Flags")]
    public bool isEnraged;
    public bool isRetreating;
    public bool isEngaging;
    public bool isFacingRight;

    // Game time tracking for conditions
    public float gameStartTime;

    private Dictionary<string, object> data = new Dictionary<string, object>();
    
    public void SetData(string key, object value)
    {
        data[key] = value;
    }
    
    public void Set(string key, object value)
    {
        data[key] = value;
    }
    
    public T Get<T>(string key)
    {
        if (data.TryGetValue(key, out object value))
        {
            return (T)value;
        }
        return default;
    }

    // Helper methods for condition checking
    public bool IsHealthBelowPercent(float percent)
    {
        return hp < maxHp * percent;
    }


}