using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private string transitionToScene;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Vector2 exitDirection;
    [SerializeField] private float exitTime;
    private bool isActive = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActive)
        {
            isActive = true;
            exitDirection = PlayerController.Instance.playerRb.linearVelocity;
            GameManager.Instance.setTransitionPoint(startPoint.position, exitDirection);
            NextLevel();            
        }
    }
    void NextLevel()
    {
        StartCoroutine(GameManager.Instance.FadeAndLoadScene(transitionToScene));
    }
}
