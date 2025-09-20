using UnityEngine;

public class ArrowController : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    private Vector2 direction;
    private Transform target;

    public void Init(Vector2 dir, Transform tgt)
    {
        direction = dir;
        target = tgt;
        Destroy(gameObject, 3f); // Destroy after 3 seconds if no hit
    }

    void Update()
    {
        transform.position += (Vector3)direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (target != null && other.transform == target && other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.EnemyHit(damage, direction, 5f);
            }
            Destroy(gameObject);
        }
        // Optionally: destroy arrow if it hits any obstacle
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            Destroy(gameObject);
        }
    }
}
