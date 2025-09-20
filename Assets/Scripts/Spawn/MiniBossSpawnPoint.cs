using UnityEngine;

public class MiniBossSpawnPoint : MonoBehaviour
{
    [Header("MiniBoss Settings")]
    public GameObject miniBossPrefab;
    public float respawnDelay = 5f;

    private GameObject currentBoss;
    private float respawnTimer = 0f;

    void Start()
    {
        SpawnMiniBoss();
    }

    void Update()
    {
        // Nếu boss đã bị xoá khỏi Hierarchy
        if (currentBoss == null)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnDelay)
            {
                SpawnMiniBoss();
                respawnTimer = 0f;
            }
        }
    }

    void SpawnMiniBoss()
    {
        currentBoss = Instantiate(miniBossPrefab, transform.position, Quaternion.identity);

        // Gắn reference spawnpoint cho boss
        MiniBossController controller = currentBoss.GetComponent<MiniBossController>();
        if (controller != null)
        {
            controller.blackboard.spawnPoint = this;
        }

        Debug.Log("MiniBoss spawned.");
    }
}
