using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Basic Stats:")]
    [SerializeField] protected float health;
    [SerializeField] protected bool isFacingRight = false;
    //[SerializeField] protected float attackCooldown = 2f;
    [SerializeField] protected AudioClip dmgSoundClip;

    protected PlayerController player;
    public float speed;
    public float damage = 1f;
    protected float lastAttackUpdateTime;

    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Animator anim;

    protected enum EnemyStates
    {
        //Crawler
        Crawler_Idle,
        Crawler_Stunned,

        //Common
        IDLE,
        PATROL,
        CHASE,
        STUNNED,
        ATTACK,
        DEATH,
        RETURN_TO_START,

        //Bat
        Bat_Idle, 
        Bat_Chase,
        Bat_Stunned, 
        Bat_Death,
        Bat_ReturnToStart,

        //Dracula
        Dracula_Stage1,
        Dracula_Stage2
    } 
    protected EnemyStates currentEnemyState;

    protected virtual EnemyStates GetCurrentEnemyState
    {
        get { return currentEnemyState; }
        set
        {
            if (currentEnemyState != value)
            {
                currentEnemyState = value;

                ChangeCurrentAnimation();
            }
        }
    }
    protected virtual void Awake()
    {
        
    }
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }
    protected virtual void Update()
    {   
        UpdateEnemyState();
    }

    public virtual void EnemyHit(float damage, Vector2 hitDirection, float hitForce)
    {
        health -= damage;
        SoundFXManager.instance.PlaySoundFX(dmgSoundClip, transform, 1f);
    }

    public void Heal(float amount)
    {
        health += amount;
    }

    protected void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Attack();
        }
    }
    protected virtual void UpdateEnemyState() { }
    protected virtual void ChangeCurrentAnimation() { }
    protected void ChangeState(EnemyStates newState)
    {
        GetCurrentEnemyState = newState;
    }
    protected virtual void Attack(){}
    protected virtual void PerformAttackTarget(Transform target)
    {
        if (target == null) return;
        if (target.CompareTag("Player"))
        {
            PlayerController.Instance.TakeDamage(damage, rb.transform.position);
        }
        else if (target.CompareTag("Ally"))
        {
            var ally = target.GetComponent<AllyUnitController>();
            if (ally != null)
            {
                ally.TakeDamage(damage, rb.transform.position);
            }
        }
    }
    protected virtual void DestroyObject()
    {
        Destroy(gameObject);
    }
}
