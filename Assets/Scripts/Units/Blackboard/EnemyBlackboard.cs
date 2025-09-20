using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyBlackboard
{
    public Transform targetPlayer;
    public Transform targetNPC;
    public Transform targetStructure;
    public List<Transform> nearbyAllies = new();
    public List<Transform> detectedEnemies = new();

    public int hp;
    public int hpMax;
    public int hpLowThreshold;

    public float attackRange;
    public float detectionRange;
    public float moveSpeed;
    public float lastAttackTime;
    public float attackCooldown;

    public bool waveActive;
    public bool commandIssued;
    public bool isEnraged;
    public bool isRetreating;
    public bool isEngaging;
    public bool isAttacking;
    public bool isScouting;

    public MiniBossSpawnPoint spawnPoint;


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
}

// MiniBossBlackboard inherits from EnemyBlackboard and can be extended with more fields/logic
public class MiniBossBlackboard : EnemyBlackboard
{
    public bool hasBuffedAllies;
    public bool hasEnraged;
    public bool hasRetreated;
    // Add more MiniBoss-specific fields as needed
}
