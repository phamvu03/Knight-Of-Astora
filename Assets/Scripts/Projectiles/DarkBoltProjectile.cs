using UnityEngine;

public class DarkBoltProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float damage = 30f;
    public float speed = 12f;
    public float lifetime = 5f;
    
    [Header("Effects")]
    public ParticleSystem hitEffect;
    public AudioClip hitSound;
    
    private Rigidbody2D rb;
    private AudioSource audioSource;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        
        // Auto destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit player or ally unit
        if (other.CompareTag("Player"))
        {
            // Deal damage to player
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage, transform.position);
            }
            
            HitTarget();
        }
        else if (other.CompareTag("Ally"))
        {
            // Deal damage to ally unit
            AllyUnitController ally = other.GetComponent<AllyUnitController>();
            if (ally != null)
            {
                ally.TakeDamage(damage, transform.position);
            }
            
            HitTarget();
        }
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            // Hit obstacle
            HitTarget();
        }
    }

    private void HitTarget()
    {
        // Play hit effect
        if (hitEffect != null)
        {
            ParticleSystem effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }
        
        // Play hit sound
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        // Destroy projectile
        Destroy(gameObject, 0.1f);
    }
}