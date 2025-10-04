using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.Image;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    
    [Header("Player state")]
    [SerializeField] private float playerSpeed;
    public PlayerState playerState;
    private float horizontalInput;
    
    [Header("Camera Stuff")]
    [SerializeField] private GameObject _cameraFollowGO;

    [Header("Roll")]
    [SerializeField] private float rollForce;
    private float rollDuration = 0.5f;
    private float rollCurrentTime;

    [Header("Dash")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    private bool canDash = true;
    private bool dashed;
    private float gravity;

    [Header("Attack")]
    [SerializeField] private float damage = 1f;
    [SerializeField] Transform SideAttackTransform;
    [SerializeField] Vector2 SideAttackArea;
    [SerializeField] LayerMask attackableLayer;
    [SerializeField] AudioClip attackAudioClip;
    private int currentAttack = 0;
    private float timeSinceAttack;

    [Header("Jump")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundCheckLayer;
    [SerializeField] private LayerMask waterCheckLayer;
    [SerializeField] private Vector2 boxSize;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private float jumpForce;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJump;

    [Header("Wall Sliding")]
    [SerializeField] private Transform[] wallCheckPoints;
    [SerializeField] private float circleRadius;
    [SerializeField] private LayerMask wallCheckLayer;
    [SerializeField] GameObject slideDust;

    [Header("Recoil")]
    [SerializeField] int recoilXStep = 5;
    [SerializeField] float recoilXSpeed = 100;
    int stepsXRecoiled;

    [Header("Heal")]
    public int health;
    [SerializeField] private int maxHealth;
    [SerializeField] float timeToHeal;
    [SerializeField] private HealController healController;
    [SerializeField] private ParticleSystem dmgParticles;
    private float healTimer;
    private ParticleSystem dmgParticlesInstance;

    [Header("Mana")]
    [SerializeField] float mana;
    [SerializeField] float manaDrainSpeed;
    [SerializeField] float manaGain;
    [SerializeField] private ManaController manaController;

    public Rigidbody2D playerRb {  get; private set; }
    private Animator playerAnimation;
    private BoxCollider2D playerBC;

    //Alter setting to combine old code
    float angleInRadian;
    float alterLocalScale;

    //Camera
    private CameraFollowObject _cameraFollowObject;
    private float _fallSpeedYDampingChangeThreshold;

    private bool isCommandingAllies = false;
    private List<AllyUnitController> commandedAllies = new List<AllyUnitController>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playerAnimation = GetComponent<Animator>();
        playerBC = GetComponent<BoxCollider2D>();
        healController = HealController.Instance;
        manaController = ManaController.Instance;

        gravity = playerRb.gravityScale;

        Health = maxHealth;
        healController.SetMaxHealth(Health);

        Mana = mana;
        manaController.SetMana(Mana);

        _cameraFollowObject = _cameraFollowGO.GetComponent<CameraFollowObject>();
        _fallSpeedYDampingChangeThreshold = CameraManager.instance.fallSpeedYDampingChangeThreshold;
    }

    void Update()
    {
        //if (playerState.cutscene) return;
        if (playerState.alive)
        {
            horizontalInput = Input.GetAxis("Horizontal");
        }
        // Set speed when player in the air
        playerAnimation.SetFloat("AirSpeedY", playerRb.velocity.y);

        // Increase timer that controls attack combo
        timeSinceAttack += Time.deltaTime;

        // Increase timer that checks roll duration
        if (playerState.isRolling)
            rollCurrentTime += Time.deltaTime;
        // Disable rolling if timer extends duration
        if (rollCurrentTime > rollDuration)
        {
            playerState.isRolling = false;
            rollCurrentTime = 0f;
        }
        //note: turn this on when back to normal
        if (playerState.alive && !PauseMenu.instance.IsPause)
        {
            Water();
            Grounded();
            WallSliding();
            Flip();
            Move();
            Jump();
            Attack();
            Roll();
            StartDash();
            Heal();
        }
        CameraSetting();

        //replace localScale when compute in roll & dash 
        angleInRadian = transform.eulerAngles.y * Mathf.Deg2Rad;
        alterLocalScale = Mathf.Cos(angleInRadian);

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isCommandingAllies)
            {
                // Find all AllyUnit in area (use OverlapCircle or custom zone logic)
                float commandRadius = 12f; // You can adjust this value
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, commandRadius, LayerMask.GetMask("Ally"));
                commandedAllies.Clear();
                foreach (var hit in hits)
                {
                    AllyUnitController ally = hit.GetComponent<AllyUnitController>();
                    if (ally != null)
                    {
                        ally.FollowPlayer(this.transform);
                        commandedAllies.Add(ally);
                    }
                }
                isCommandingAllies = true;
            }
            else
            {
                // Cancel follow for all commanded allies
                foreach (var ally in commandedAllies)
                {
                    if (ally != null)
                        ally.CancelFollowPlayer();
                }
                commandedAllies.Clear();
                isCommandingAllies = false;
            }
        }
    }
    private void FixedUpdate()
    {
        Recoil();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.color = IsOnGround() ? Color.green : Color.red;
        Gizmos.DrawWireCube(groundCheckPoint.position + Vector3.down * groundCheckDistance / 2, new Vector3(boxSize.x, boxSize.y, 1));
        Gizmos.color = IsWallSliding() ? Color.green : Color.red;
        for (int i = 0; i < wallCheckPoints.Length; i++)
        {
            Gizmos.DrawWireSphere(wallCheckPoints[i].position, circleRadius);
        }
    }
    public bool IsOnGround()
    {
        if (Physics2D.BoxCast(groundCheckPoint.position, boxSize, 0, Vector2.down, groundCheckDistance, groundCheckLayer))
            return true;
        else return false;
    }
    private void Grounded()
    {
        if (IsOnGround())
        {
            playerAnimation.SetBool("Grounded", true);
            airJumpCounter = 0;
            dashed = false;
            playerState.isJumping = false;
        }
        else
        {
            playerAnimation.SetBool("Grounded", false);
        }
    }
    bool IsWallSliding()
    {
        bool m_wallSensorR1 = Physics2D.OverlapCircle(wallCheckPoints[0].position, circleRadius, wallCheckLayer);
        bool m_wallSensorR2 = Physics2D.OverlapCircle(wallCheckPoints[1].position, circleRadius, wallCheckLayer);
        bool m_wallSensorL1 = Physics2D.OverlapCircle(wallCheckPoints[2].position, circleRadius, wallCheckLayer);
        bool m_wallSensorL2 = Physics2D.OverlapCircle(wallCheckPoints[3].position, circleRadius, wallCheckLayer);

        if ((m_wallSensorR1 && m_wallSensorR2) || (m_wallSensorL1 && m_wallSensorL2))
            return true;
        else return false;
    }
    void WallSliding()
    {
        if(IsWallSliding())
        {
            playerAnimation.SetBool("WallSlide", true);
            airJumpCounter = 0;
            dashed = false;
            playerState.isJumping = false;
        }
        else
            playerAnimation.SetBool("WallSlide", false);
    }
    void CameraSetting()
    {
        //if player is falling past a certain speed threshold
        if(playerRb.velocity.y < _fallSpeedYDampingChangeThreshold && !CameraManager.instance.IsLerpingYDamping && !CameraManager.instance.LerpedFromPlayerFalling)
        {
            CameraManager.instance.LerpYDamping(true);
        }
        //if player is standing still or moving up
        if(playerRb.velocity.y >= 0 && !CameraManager.instance.IsLerpingYDamping && CameraManager.instance.LerpedFromPlayerFalling)
        {
            //reset so it can be called again
            CameraManager.instance.LerpedFromPlayerFalling = false;

            CameraManager.instance.LerpYDamping(false);
        }
    }
    private void Water()
    {
        if (Physics2D.BoxCast(groundCheckPoint.position, boxSize, 0, Vector2.down, groundCheckDistance, waterCheckLayer))
        {
            playerState.alive = false;
            StartCoroutine(Death());
        }
    }
    private void Flip()
    {
        if (horizontalInput > 0 && !playerState.lookingRight)
        {
            //transform.localScale = new Vector2(1, transform.localScale.y);
            Vector3 rotator = new Vector3(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            playerState.lookingRight = true;

            //Turn the camera follow object
            _cameraFollowObject.CallTurn();
        }
        else if (horizontalInput < 0 && playerState.lookingRight)
        {
            //transform.localScale = new Vector2(-1, transform.localScale.y);
            Vector3 rotator = new Vector3(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            playerState.lookingRight = false;

            //Turn the camera follow object
            _cameraFollowObject.CallTurn();
        }
    }
    private void Move()
    {
        if (!playerState.isRolling && !playerState.isDashing && !playerState.isHealing)
        {
            playerRb.velocity = new Vector2(horizontalInput * playerSpeed, playerRb.velocity.y);    
        }

        if (horizontalInput != 0)
        {
            playerAnimation.SetFloat("Speed", 1);
        }
        else
        {
            playerAnimation.SetFloat("Speed", 0);
        }
    }
    private void Jump()
    {
        if (Input.GetKeyUp(KeyCode.Space) && playerRb.velocity.y > 0)
        {
            playerAnimation.SetTrigger("Jump");
            playerRb.velocity = new Vector2(playerRb.velocity.x, 0);
        }

        if (Input.GetKeyDown(KeyCode.Space) && IsOnGround() && !playerState.isRolling && !playerState.isHealing)
        {
            playerState.isJumping = true;
            playerAnimation.SetTrigger("Jump");
            playerRb.velocity = new Vector2(playerRb.velocity.x, jumpForce);
        }
        else if (!IsOnGround() && airJumpCounter < maxAirJump && Input.GetKeyDown(KeyCode.Space))
        {
            airJumpCounter++;
            playerAnimation.SetTrigger("Jump");
            playerRb.velocity = new Vector2(playerRb.velocity.x, jumpForce);
        }
    }
    void Attack()
    {
        float attackInterval = 1f / playerState.attackSpeed;
        if (Input.GetMouseButtonDown(0) && timeSinceAttack > attackInterval && !playerState.isRolling && !playerState.isHealing)
        {
            currentAttack++;
            SoundFXManager.instance.PlaySoundFX(attackAudioClip, transform, 1f);
            if (currentAttack > 3)
                currentAttack = 1;

            if (timeSinceAttack > 1f)
                currentAttack = 1;

            playerAnimation.SetTrigger("Attack" + currentAttack);
            timeSinceAttack = 0f;

            int recoilLeftOrRight = playerState.lookingRight ? 1 : -1;
            Hit(SideAttackTransform, SideAttackArea, ref playerState.recoilingX, Vector2.right * recoilLeftOrRight, recoilXSpeed);
        }
    }
    void Hit(Transform attackTransform, Vector2 attackArea, ref bool recoilBool, Vector2 recoilDir, float recoilStrength)
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(attackTransform.position, attackArea, 0, attackableLayer);
        List<Enemy> hitEnemies = new List<Enemy>();
        if (objectsToHit.Length > 0)
        {
            recoilBool = true;
        }
        for (int i = 0; i < objectsToHit.Length; i++)
        {
            if (objectsToHit[i].GetComponent<Enemy>() != null)
            {
                objectsToHit[i].GetComponent<Enemy>().EnemyHit(damage, recoilDir, recoilStrength);
                if (objectsToHit[i].CompareTag("Enemy"))
                {
                    Mana += manaGain;
                    manaController.SetMana(Mana);
                }
            }
            if (objectsToHit[i].GetComponent<MiniBossController>() != null)
            {
                objectsToHit[i].GetComponent<MiniBossController>().TakeDamage(damage);
                if (objectsToHit[i].CompareTag("Enemy"))
                {
                    Mana += manaGain;
                    manaController.SetMana(Mana);
                }
            }
            if (objectsToHit[i].GetComponent<SpawnBase>() != null)
            {
                objectsToHit[i].GetComponent<SpawnBase>().TakeDamage(damage);
            }
        }
    }
    void Recoil()
    {
        if (playerState.recoilingX)
        {
            if (playerState.lookingRight)
            {
                playerRb.velocity = new Vector2(-recoilXSpeed, 0);
            }
            else playerRb.velocity = new Vector2(recoilXSpeed, 0);
        }

        //Stop Recoil
        if (playerState.recoilingX && stepsXRecoiled < recoilXStep)
            stepsXRecoiled++;
        else StopRecoilX();
    }
    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        playerState.recoilingX = false;
    }
    public void TakeDamage(float damage, Vector2 attackPos)
    {
        if (playerState.alive)
        {
            Health -= Mathf.RoundToInt(damage);
            healController.SetHealth(Health);
            if (Health <= 0)
            {
                Health = 0;
                StartCoroutine(Death());
            }
            else
            {
                StartCoroutine(StopTakingDamage());
                SpawnDmgParticles(attackPos);
            }
        }
    }
    IEnumerator StopTakingDamage()
    {
        playerState.invincible = true;
        playerAnimation.SetTrigger("Hurt");
        yield return new WaitForSeconds(1f);
        playerState.invincible = false;
    }
    public int Health
    {
        get { return health; }
        set
        {
            if (health != value)
            {
                health = Mathf.Clamp(value, 0, maxHealth);
            }
        }
    }
    void SpawnDmgParticles(Vector2 attackPos)
    {
        Vector2 attackDir = new Vector2(transform.position.x - attackPos.x, 0).normalized + new Vector2(0, 1.5f);
        Quaternion spawnQuaternion = Quaternion.FromToRotation(Vector2.right, attackDir);
        dmgParticlesInstance = Instantiate(dmgParticles, transform.position, spawnQuaternion);
    }
    IEnumerator IFrame()
    {
        playerState.invincible = true;
        yield return new WaitForSeconds(rollDuration);
        playerState.invincible = false;
    }
    void Heal()
    {
        if (Input.GetMouseButton(1) && Health < maxHealth && !playerState.isRolling && Mana > 0 
            && !playerState.isDashing && !playerState.isJumping && IsOnGround())
        {
            playerRb.velocity = Vector3.zero;
            playerState.isHealing = true;
            healTimer += Time.deltaTime;
            playerAnimation.SetBool("Heal", true);
            
            if (healTimer >= timeToHeal)
            {
                Health++;
                healController.SetHealth(Health);
                healTimer = 0;
            }

            Mana -= Time.deltaTime * manaDrainSpeed;
            manaController.SetMana(Mana);
        }
        else
        {
            playerState.isHealing = false;
            playerAnimation.SetBool("Heal", false);
            healTimer = 0;
        }
    }
    float Mana
    {
        get { return mana; }
        set
        {
            if (mana != value)
            {
                mana = Mathf.Clamp(value, 0, 1);
            }
        }
    }
    private void Roll()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && Mana >= 0.3f && IsOnGround() && !playerState.isRolling 
            && !playerState.isDashing && !playerState.isHealing)
        {
            playerState.isRolling = true;
            playerAnimation.SetTrigger("Roll");
            Mana -= 0.3f;
            manaController.SetMana(Mana);
            StartCoroutine(IFrame());
            playerRb.velocity = new Vector2(rollForce * alterLocalScale, 0);
        }
    }
    private void StartDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && canDash && !dashed && !playerState.isRolling && !playerState.isHealing)
        {
            StartCoroutine(Dash());
            dashed = true;
        }
    }
    IEnumerator Dash()
    {
        canDash = false;
        playerState.isDashing = true;
        playerAnimation.SetTrigger("Dash");
        playerRb.gravityScale = 0;
        playerRb.velocity = new Vector2(alterLocalScale * dashSpeed, 0);
        yield return new WaitForSeconds(dashTime);
        playerRb.gravityScale = gravity;
        playerState.isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    IEnumerator Death()
    {
        playerState.alive = false;
        playerRb.velocity = Vector2.zero;
        playerAnimation.SetTrigger("Death");

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(UIManager.Instance.ActiveDeathScreen());
    }
    public void Respawn()
    {
        if (!playerState.alive)
        {
            playerState.alive = true;
            Health = maxHealth;
            healController.SetMaxHealth(maxHealth);
            playerAnimation.Play("Idle");
        }
    }
    public void RespawnAtDefault(Vector2 respawnPos)
    {
        transform.position = respawnPos;
        playerRb.velocity = Vector2.zero;
        Respawn();
    }
    public IEnumerator WalkIntoNewScene(Vector3 spawnPosition, Vector2 exitDir, float delay)
    {
        transform.position = spawnPosition;
        if (exitDir.y != 0)
        {
            playerRb.velocity = exitDir;
        }
        if (exitDir.x != 0)
        {
            horizontalInput = exitDir.x > 0 ? 1 : -1;

            Move();
        }
        yield return new WaitForSeconds(delay);
    }
    void AE_SlideDust()
    {
        Vector3 spawnPosition;

        if (playerState.lookingRight == true)
            spawnPosition = wallCheckPoints[1].transform.position;
        else
            spawnPosition = wallCheckPoints[3].transform.position;

        if (slideDust != null)
        {
            // Set correct arrow spawn position
            GameObject dust = Instantiate(slideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
            // Turn arrow in correct direction
            dust.transform.localScale = new Vector3((playerState.lookingRight ? 1 : -1), 1, 1);
        }
    }
}
