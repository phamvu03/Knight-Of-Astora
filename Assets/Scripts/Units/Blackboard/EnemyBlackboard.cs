using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyBlackboard
{
    [Header("Detect enemy")]
    public Transform currentTarget;
    public List<Transform> detectedEnemies = new();

    [Header("Health")]
    public int hp;
    public int maxHp;
    public int hpLowThreshold;

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

    public float GetGameTime()
    {
        return Time.time - gameStartTime;
    }
}

// MiniBossBlackboard inherits from EnemyBlackboard and can be extended with more fields/logic
public class MiniBossBlackboard : EnemyBlackboard
{
    // Spawn point reference
    public MiniBossSpawnPoint spawnPoint;

    // Additional MiniBoss specific variables
    public int phaseLevel = 1; // Boss phases (1, 2, 3)
    public bool hasRetreatedOnce;
    public int totalUnitsSpawned;
    public float lastPhaseChangeTime;

    // Special abilities unique to MiniBoss
    public bool canTeleport = true;
    public float teleportCooldown = 15f;
    public float lastTeleportTime;

    // Spell/ability cooldowns
    public float lastDarkBoltTime;
    public float darkBoltCooldown = 3f;
    public float darkBoltRange = 8f;
    public int darkBoltDamage = 30;

    // Wave management
    public bool waveActive;
    public bool commandIssued;

    // Summoning system
    public int currentArmySize;
    public int maxArmySize = 12;
    public float lastSummonTime;
    public float summonCooldown = 8f;
    public int skeletonsPerSummon = 2;
    public float summonRange = 3f;

    // Buff system
    public float lastBuffTime;
    public float buffCooldown = 10f;
    public float buffRadius = 5f;
    public float buffDamageMultiplier = 1.5f;
    public float buffHealAmount = 20f;
    public bool hasBuffedAllies;

    // Area control and advancement
    public Vector3 currentAreaCenter;
    public Vector3 nextAreaCenter;
    public float areaRadius = 10f;
    public bool readyToAdvance;
    public bool hasCommandedArmy;
    public float lastAreaAdvanceTime;

    [Header("Detect ally")]
    public float allyDetectionRange;
    public List<Transform> nearbyAllies = new();
    public LayerMask allyLayerMask;

    public MiniBossBlackboard()
    {
        // Set MiniBoss specific defaults
        maxHp = 200;
        hp = 200;
        hpLowThreshold = 60; // 30% of max health
        maxArmySize = 12;
        attackRange = 8f;
        detectionRange = 12f;
        moveSpeed = 3f;
        gameStartTime = Time.time;
    }

    public void Initialize(Transform miniBossTransform)
    {
        spawnPosition = miniBossTransform.position;
        hp = maxHp;
    }

    public bool HasDetectedEnemies()
    {
        return detectedEnemies.Count > 0;
    }

    public bool CanAdvance()
    {
        return readyToAdvance && Time.time > lastAreaAdvanceTime + 30f;
    }

    public bool ArmySizeBelowLimit()
    {
        return currentArmySize < maxArmySize;
    }

    public bool CanSummonSkeletons()
    {
        return Time.time >= lastSummonTime + summonCooldown && currentArmySize < maxArmySize;
    }

    public bool IsArmySizeBelowLimit(int limit)
    {
        return currentArmySize < limit;
    }

    public bool CanSummon()
    {
        return Time.time >= lastSummonTime + (isFrenzyActive ? summonCooldown * cooldownReductionFactor : summonCooldown)
               && currentArmySize < maxArmySize;
    }

    public bool CanBuffAllies()
    {
        return Time.time >= lastBuffTime + (isFrenzyActive ? buffCooldown * cooldownReductionFactor : buffCooldown);
    }

    public bool CanCastDarkBolt()
    {
        return Time.time >= lastDarkBoltTime + (isFrenzyActive ? darkBoltCooldown * cooldownReductionFactor : darkBoltCooldown);
    }
}
