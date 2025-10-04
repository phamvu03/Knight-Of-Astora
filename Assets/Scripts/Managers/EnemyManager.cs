using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private static EnemyManager _instance;
    public static EnemyManager Instance => _instance;

    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    // Add an enemy to the list
    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    // Remove an enemy from the list
    public void UnregisterEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    // Get the current number of active enemies
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }

    // Optional: Get all active enemies
    public List<GameObject> GetAllActiveEnemies()
    {
        return new List<GameObject>(activeEnemies);
    }
}