using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyBlackboard
{
    // Target tracking
    public Transform targetPlayer;
    public Transform targetNPC;
    public Transform targetStructure;
    public List<Transform> nearbyAllies = new();
    public List<Transform> detectedEnemies = new();

    // Health and combat stats
    public int hp;
    public int hpMax;
    public int hpLowThreshold;
    public float attackRange;
    public float detectionRange;
    public float moveSpeed;
    public float lastAttackTime;
    public float attackCooldown;

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

    // Spell/ability cooldowns
    public float lastDarkBoltTime;
    public float darkBoltCooldown = 3f;
    public float darkBoltRange = 8f;
    public int darkBoltDamage = 30;

    // Area control and advancement
    public Vector3 currentAreaCenter;
    public Vector3 nextAreaCenter;
    public float areaRadius = 10f;
    public bool readyToAdvance;
    public bool hasCommandedArmy;
    public float lastAreaAdvanceTime;

    // Retreat and healing
    public bool isAtSpawn;
    public float healAtSpawnRate = 5f; // HP per second when at spawn
    public float retreatSpeed = 6f;
    public Vector3 spawnPosition;

    // Frenzy mode
    public bool isFrenzyActive;
    public float frenzyStartTime;
    public float frenzyDuration = 30f;
    public float cooldownReductionFactor = 0.5f; // Reduces all cooldowns by 50% in frenzy

    // State flags
    public bool waveActive;
    public bool commandIssued;
    public bool isEnraged;
    public bool isRetreating;
    public bool isEngaging;
    public bool isAttacking;
    public bool isScouting;

    // Spawn point reference
    public MiniBossSpawnPoint spawnPoint;

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
        return hp < hpMax * percent;
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

    public float GetGameTime()
    {
        return Time.time - gameStartTime;
    }
}

// MiniBossBlackboard inherits from EnemyBlackboard and can be extended with more fields/logic
public class MiniBossBlackboard : EnemyBlackboard
{
    // Additional MiniBoss specific variables
    public int phaseLevel = 1; // Boss phases (1, 2, 3)
    public bool hasRetreatedOnce;
    public int totalUnitsSpawned;
    public float lastPhaseChangeTime;
    
    // Special abilities unique to MiniBoss
    public bool canTeleport = true;
    public float teleportCooldown = 15f;
    public float lastTeleportTime;
    
    public MiniBossBlackboard()
    {
        // Set MiniBoss specific defaults
        hpMax = 200;
        hp = 200;
        hpLowThreshold = 60; // 30% of max health
        maxArmySize = 12;
        attackRange = 8f;
        detectionRange = 12f;
        moveSpeed = 3f;
        gameStartTime = Time.time;
    }
}
