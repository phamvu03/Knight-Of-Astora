using UnityEngine;

public class AbilitiesScript : MonoBehaviour
{
    public float speed = 8f;
    public float explosionRadius = 2f;
    public float damage = 50f;
    public LayerMask targetLayer;

    private Vector2 direction;

    public void Launch(Vector2 dir)
    {
        if(dir.x < 0)
        {
            Flip();
        }
        direction = dir.normalized;
    }
    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Explode();
    }

    void Explode()
    {
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
        Destroy(gameObject, 1f);
    }
}