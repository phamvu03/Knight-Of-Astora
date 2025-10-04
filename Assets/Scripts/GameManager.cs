using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    //respawn variables
    [SerializeField] private Transform player;
    private Vector3 playerDefaultRespawnPos;
    private int defaultSceneIndex = 1;
    private string checkpointSceneName; 
    private Vector2 checkpointPosition;

    //scene transition variables
    [SerializeField] private Animator transitionAnim;
    private Vector2 transitionPos;
    private Vector2 transitionDir;
    private float delay = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        playerDefaultRespawnPos = player.position;
    }
    public void FadeIn()
    {
        transitionAnim.SetTrigger("Start");
    }
    public void FadeOut()
    {
        transitionAnim.SetTrigger("End");
    }
    public void SetCheckpoint(string sceneName, Vector2 position)
    {
        checkpointSceneName = sceneName;
        checkpointPosition = position;

        Debug.Log($"Checkpoint set! Scene: {sceneName}, Position: {position}");
    }
    public void setTransitionPoint(Vector2 position, Vector2 direction)
    {
        transitionPos = position;
        transitionDir = direction;
    }
    public void RespawnPlayer()
    {
        if (!string.IsNullOrEmpty(checkpointSceneName))
        {
            if (SceneManager.GetActiveScene().name != checkpointSceneName)
            {
                SceneManager.LoadSceneAsync(checkpointSceneName);
                StartCoroutine(RespawnAfterSceneLoad());
            }
            else
            {
                RespawnAtCheckpoint();
            }
        }
        else
        {
            if (SceneManager.GetActiveScene().name != checkpointSceneName)
            {
                SceneManager.LoadSceneAsync(defaultSceneIndex);
                StartCoroutine(RespawnAfterSceneLoad());
            }
            else
            {
                RespawnAtCheckpoint();
            }
        }
    }
    private IEnumerator RespawnAfterSceneLoad()
    {
        FadeIn();
        yield return new WaitForSeconds(1f);
        if (!string.IsNullOrEmpty(checkpointSceneName))
        {
            RespawnAtCheckpoint();
        }
        else
        {
            RespawnAtDefault();
        }
    }
    private void RespawnAtCheckpoint()
    {
        PlayerController.Instance.transform.position = checkpointPosition;
        StartCoroutine(UIManager.Instance.DeactiveDeathScreen());
        PlayerController.Instance.Respawn();
    }
    private void RespawnAtDefault()
    {
        PlayerController.Instance.transform.position = playerDefaultRespawnPos;
        StartCoroutine(UIManager.Instance.DeactiveDeathScreen());
        PlayerController.Instance.Respawn();
    }
    public IEnumerator FadeAndLoadScene(string sceneName)
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(1f);
        SceneManager.LoadSceneAsync(sceneName);
        transitionAnim.SetTrigger("Start");
        StartCoroutine(PlayerController.Instance.WalkIntoNewScene(transitionPos, transitionDir, delay));
    }

}
