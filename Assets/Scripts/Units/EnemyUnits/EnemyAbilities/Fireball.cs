using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 8f;
    public float explosionRadius = 2f;
    public float damage = 50f;
    public LayerMask targetLayer;

    private Vector2 direction;

    public void Launch(Vector2 spawnPosition, Vector2 targetPosition)
    {
        // Set the initial position of the fireball
        transform.position = spawnPosition;

        // Calculate the direction from the spawn position to the target position
        direction = (targetPosition - spawnPosition).normalized;

        // Rotate the fireball to face the target direction
        if(direction.x < 0)
        {
            Flip();
        }
    }

    void Update()
    {
        // Move the fireball in the calculated direction
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Explode();
    }

    void Explode()
    {
        // Detect all objects within the explosion radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<PlayerController>() != null)
            {
                hit.GetComponent<PlayerController>().TakeDamage(damage, direction);
            }
            if (hit.GetComponent<AllyUnitController>() != null)
            {
                hit.GetComponent<AllyUnitController>().TakeDamage(damage, direction);
            }
        }

        // Destroy the fireball after the explosion
        Destroy(gameObject, 1f);
    }
    private void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}