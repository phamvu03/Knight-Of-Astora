using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

public class BossScript : Enemy
{

    public static BossScript instance;

    [Header("Attack")]
    public Transform SideAttackTransform;
    public Vector2 SideAttackArea;
    public float attackRange;
    public float attackTimer;

    [HideInInspector] public bool facingLeft;

    [Header("Ground Check Settings:")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundCheckLayer;
    [SerializeField] private Vector2 boxSize;
    [SerializeField] private float groundCheckDistance;

    bool stunned, canStun;
    bool alive;

    [Header("Movement")]
    public float jumpForce;
    public float jumpDistance;

    [Header("Abilities")]
    [SerializeField] private Transform abilitiesSpawn;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawWireCube(groundCheckPoint.position + new Vector3(-0.05f, 0, 0) +
            Vector3.down * groundCheckDistance / 2, new Vector3(boxSize.x, boxSize.y, 1));
    }
    public bool IsOnGround()
    {
        if (Physics2D.BoxCast(groundCheckPoint.position, boxSize, 0,
            Vector2.down, groundCheckDistance, groundCheckLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public void Flip()
    {
        if (PlayerController.Instance.transform.position.x < transform.position.x
            && transform.localScale.x > 0)
        {
            transform.eulerAngles = new Vector2(transform.eulerAngles.x, 0);
            facingLeft = true;
        }
        else
        {
            transform.eulerAngles = new Vector2(transform.eulerAngles.x, 180);
            facingLeft = false;
        }
    }
    protected override void UpdateEnemyState()
    {
        if (PlayerController.Instance != null) 
        {
            switch (GetCurrentEnemyState)
            {
                case EnemyStates.Dracula_Stage1:
                    attackTimer = 3f;
                    break;
                case EnemyStates.Dracula_Stage2:
                    attackTimer = 2f;
                    break;
            }
        }

    }
    protected override void Awake()
    {
        base.Awake();
        if (instance != null && instance != this)
            Destroy(gameObject);
        else
            instance = this;
    }
    protected override void Start()
    {
        base.Start();
        anim = GetComponentInChildren<Animator>();
        ChangeState(EnemyStates.Dracula_Stage1);
        alive = true;
    }
    
    protected override void Update()
    {
        base.Update();

        if (health <= 0 && alive)
        {
            Death(0);
        }
        if (!attacking)
        {
            attackCountDown -= Time.deltaTime;
        }
    }
    private void OnCollisionStay2D(Collision2D other)
    {
        
    }
    #region attacking
    #region variable
    [HideInInspector] public bool jump;
    [HideInInspector] public bool attacking;
    [HideInInspector] public float attackCountDown;
    [HideInInspector] public bool damagedlayer = false;

    #endregion

    #region Control
    public void AttackHandler()
    {
        if (currentEnemyState == EnemyStates.Dracula_Stage1)
        {
            if (Vector2.Distance(PlayerController.Instance.transform.position, rb.position) <= attackRange)
            {
                BarrageBendDown();
            }
        }
        if (currentEnemyState == EnemyStates.Dracula_Stage2)
        {
            if (Vector2.Distance(PlayerController.Instance.transform.position, rb.position) <= attackRange)
            {
                TripleAttack();
            }
        }
        //TripleAttack();
        //Lunge();
    }
    public void ResetAllAttack()
    {
        attacking = false;

        StopCoroutine(Barrage());
        StopCoroutine(TripleBarrage());
        StopCoroutine(Lunge());

        fireballAttack = false;
        tripleAttack = false;
    }
    #endregion

    #region Stage 1

    [HideInInspector] public bool fireballAttack;
    public GameObject FireBall;
    
    void BarrageBendDown() 
    {
        attacking = true;
        rb.velocity = Vector2.zero;
        fireballAttack = true;
        anim.SetTrigger("Fireball");
    }
    public IEnumerator Barrage()
    {
        rb.velocity = Vector2.zero;
        float currentAngle = 0f;
        GameObject projectile = Instantiate(FireBall, abilitiesSpawn.position, Quaternion.identity);
        if (facingLeft)
        {
            projectile.transform.eulerAngles =new Vector3(projectile.transform.eulerAngles.x, 
                0, currentAngle);
        }
        else
        {
            projectile.transform.eulerAngles = new Vector3(projectile.transform.eulerAngles.x, 
                180, currentAngle);
        }
        yield return new WaitForSeconds(0.5f);
        ResetAllAttack();
    }

    #endregion

    #region Stage_2
    [HideInInspector] public bool tripleAttack;
    [HideInInspector] public bool lungeAttack;
    [HideInInspector] public bool dmgPlayer = false;
    void TripleAttack()
    {
        attacking = true;
        rb.velocity = Vector2.zero;
        tripleAttack = true;
        anim.SetTrigger("TripleFireball");
    }
    IEnumerator Lunge()
    {
        attacking = true;
        anim.SetBool("Lunge", true);
        yield return new WaitForSeconds(0.25f);
        anim.SetBool("Lunge", false);
        dmgPlayer = false;
        ResetAllAttack();
    }
    public IEnumerator TripleBarrage()
    {
        //Set boss velocity to 0
        rb.velocity = Vector2.zero;
        
        float currentAngle = 0f;
        for (int i = 0; i < 3; i++)
        {
            //Initiate fireball at abilities spawn position with currentAngle
            GameObject projectile = Instantiate(FireBall, abilitiesSpawn.position, Quaternion.Euler( 0, 0, currentAngle));
            AbilitiesScript abilitiesScript = projectile.GetComponent<AbilitiesScript>();
            float currentToRadian = (45f + currentAngle) * Mathf.Deg2Rad;
            //abilitiesScript.xAxis = -(Mathf.Sqrt(2) * Mathf.Cos(currentToRadian));
            //abilitiesScript.yAxis = -(Mathf.Sqrt(2) * Mathf.Sin(currentToRadian));
            if (facingLeft)
            {
                projectile.transform.eulerAngles = new Vector3(projectile.transform.eulerAngles.x,
                    0, currentAngle);
            }
            else
            {
                projectile.transform.eulerAngles = new Vector3(projectile.transform.eulerAngles.x,
                    180, currentAngle);
            }
            currentAngle -= 10f;
            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(0.1f);
        anim.SetBool("Cast", false);
        ResetAllAttack();
    }
    
    #endregion

    #endregion
    public override void EnemyHit(float dmgDone, Vector2 hitDirection, float hitForce)
    {
        base.EnemyHit(dmgDone, hitDirection, hitForce);
        #region healt to state
        if(health > 5)
        {
            ChangeState(EnemyStates.Dracula_Stage1);
        }
        if (health <= 5)
        {
            ChangeState(EnemyStates.Dracula_Stage2);
        }
        if(health <= 0)
        {
            //StartCoroutine(UIManager.Instance.EndGameActive());
            Death(0);
        }
        #endregion
    }
    protected void Death(float destroyTime)
    {
        ResetAllAttack();
        alive = false;
        rb.velocity = new Vector2(rb.velocity.x, -25);
        anim.SetTrigger("Die");
    }
    public void DestroyAfterDeath()
    {
        Destroy(gameObject);
    }


}
