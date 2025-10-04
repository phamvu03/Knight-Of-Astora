using UnityEngine;
using System.Collections.Generic;

public enum AllyCombatType
{
    Melee,
    Ranged,
    Support
}

public enum SurrenderType
{
    None,
    LastStand,
    Surrender
}

public class AllyBlackboard : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 8f;
    public float hearingRange = 10f;
    public float fieldOfView = 90f;
    
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;
    public float walkSpeed = 3f;
    
    [Header("Alert State")]
    public float idleAnimationInterval = 3f;
    public float lastSoundHeardTime;
    public Vector2 lastSoundPosition;
    public bool isInvestigatingSoundSource;
    
    [Header("Runtime Data")]
    public int currentPatrolIndex;
    public float lastIdleAnimationTime;
    public bool isPatrolling;
    public List<Transform> detectedEnemies = new List<Transform>();

    [Header("Warn State")]
    public bool isWarning;
    public Vector2 warnedEnemyPosition;
    public bool receivedWarnSignal;

    [Header("Engage State")]
    public AllyCombatType combatType = AllyCombatType.Ranged; // Default to Ranged for archer
    public bool isEngaging;
    public Transform currentTarget;
    public float engageRange = 6f; // For ranged, melee, etc.
    public float supportRange = 8f;

    [Header("Pursue State")]
    public float chaseRadius = 12f;
    public Vector2 lastKnownEnemyPosition;
    public bool isPursuing;
    public float searchTimer;
    public float maxSearchTime = 2.5f;

    [Header("Surrender State")]
    public bool isSurrendering;
    public bool isLastStand;
    public SurrenderType surrenderType = SurrenderType.None;
    public Transform surrenderTargetBase;
    public float surrenderChance = 0.85f; // 85% Surrender, 15% Last Stand
    public float lastStandChance = 0.15f;
    public float hpLowThreshold = 0.2f; // HP ratio threshold

    [Header("HP State")]
    public float currentHP = 5f;
    public float maxHP = 5f;

    public void ResetAlertState()
    {
        isInvestigatingSoundSource = false;
        lastSoundPosition = Vector2.zero;
    }

    public void HandleCombatSound(Vector2 soundPosition)
    {
        lastSoundPosition = soundPosition;
        lastSoundHeardTime = Time.time;
        isInvestigatingSoundSource = true;
    }

    public void ReceiveWarnSignal(Vector2 enemyPosition)
    {
        isWarning = true;
        warnedEnemyPosition = enemyPosition;
        receivedWarnSignal = true;
    }

    public void ResetWarnState()
    {
        detectedEnemies.Clear();
        isWarning = false;
        warnedEnemyPosition = Vector2.zero;
        receivedWarnSignal = false;
    }

    public void ResetEngageState()
    {
        isEngaging = false;
        currentTarget = null;
    }

    public void ResetPursueState()
    {
        isPursuing = false;
        lastKnownEnemyPosition = Vector2.zero;
        searchTimer = 0f;
    }

    public void ResetSurrenderState()
    {
        isSurrendering = false;
        isLastStand = false;
        surrenderType = SurrenderType.None;
    }
}