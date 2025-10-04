using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class MiniBossBlackboard : EnemyBlackboard
{
    // Spawn point reference
    public MiniBossSpawnPoint spawnPoint;

    [Header("Phase Management")]
    public int phaseLevel = 1; // Boss phases (1, 2)
    public bool hasRetreatedOnce;
    public int totalUnitsSpawned;
    public float lastPhaseChangeTime;

    [Header("Dark Bolt Ability")]
    public float lastDarkBoltTime;
    public float darkBoltCooldown = 3f;
    public float darkBoltRange = 8f;
    public int damage = 2;
    public GameObject darkBoltPrefab;
    public Transform darkBoltSpawnPoint;

    // Wave management
    public bool waveActive;
    public bool commandIssued;

    [Header("Summoning and Army Management")]
    public int maxArmySize = 12;
    public float lastSummonTime = -10f;
    public float summonCooldown = 30f;
    public int skeletonsPerSummon = 4;
    public float summonRange = 3f;
    public GameObject skeletonPrefab;

    [Header("Buff Allies Ability")]
    public float lastBuffTime;
    public float buffCooldown = 10f;
    public float buffRange = 5f;
    public float buffDamageMultiplier = 1.5f;
    public float buffHealAmount = 2f;
    public bool hasBuffedAllies;
    public int numberAllyHealed = 5;

    [Header("Area Advancement")]
    public Vector3 currentAreaCenter;
    public Vector3 nextAreaCenter;
    public float areaRadius = 10f;
    public bool readyToAdvance;
    public bool hasCommandedArmy;
    public float lastAreaAdvanceTime;

    [Header("Detect ally: Enemy")]
    public float allyDetectionRange;
    public List<GameObject> nearbyAllies = new();
    public LayerMask allyLayerMask;

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

    public bool CanSummonSkeletons()
    {
        int currentArmySize = GameObject.FindGameObjectsWithTag("Enemy").Length; // Use tag-based search to get current army size
        return (Time.time >= lastSummonTime + summonCooldown) && (currentArmySize < maxArmySize);
    }

    public bool CanSummon()
    {
        return Time.time >= lastSummonTime + (isFrenzyActive ? summonCooldown * cooldownReductionFactor : summonCooldown);
    }

    public bool CanBuffAllies()
    {
        return Time.time >= lastBuffTime + (isFrenzyActive ? buffCooldown * cooldownReductionFactor : buffCooldown);
    }

    public bool CanCastDarkBolt()
    {
        return Time.time >= lastDarkBoltTime + (isFrenzyActive ? darkBoltCooldown * cooldownReductionFactor : darkBoltCooldown) && currentTarget != null;
    }

    public float GetGameTime()
    {
        return Time.time - gameStartTime;
    }

    public bool ArmySizeBelowLimit()
    {
        return GameObject.FindGameObjectsWithTag("Enemy").Length < maxArmySize; // Use tag-based search to check army size limit
    }
}
