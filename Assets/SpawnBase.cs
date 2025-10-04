using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnBase : MonoBehaviour
{
    [SerializeField] private float hp;

    void Start()
    {
            
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            UIManager.Instance.StartCoroutine(UIManager.Instance.EndGameActive());
        }
    }
}
