using UnityEngine;
using UnityEngine.SceneManagement;

public class F_Logic_InitScene : MonoBehaviour
{
    [Header("Constants Private")]
    private const string initialGameplaySceneName = "Scene_TestRoom";
    private const string playerSpawnPointName = "PlayerSpawnPoint";

    [Header("Privates")]
    private F_Logic_GameManager gameManager;

    private void Awake()
    {
        gameManager = GetComponent<F_Logic_GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("F_Logic_InitScene requires F_Logic_GameManager on the same GameObject.");
            return;
        }

        SceneManager.sceneLoaded += PlacePlayerAtSpawnPoint;
    }

    private void Start()
    {
        if (gameManager != null && !SceneManager.GetSceneByName(initialGameplaySceneName).isLoaded)
        {
            SceneManager.LoadScene(initialGameplaySceneName, LoadSceneMode.Single);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= PlacePlayerAtSpawnPoint;
    }

    private void PlacePlayerAtSpawnPoint(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (loadedScene.name != initialGameplaySceneName)
        {
            return;
        }

        if (gameManager == null || gameManager.playerObject == null)
        {
            Debug.LogError("The game manager does not have a player assigned after loading the gameplay scene.");
            return;
        }

        GameObject spawnPoint = GameObject.Find(playerSpawnPointName);
        if (spawnPoint == null)
        {
            Debug.LogError($"The gameplay scene is missing a {playerSpawnPointName} GameObject.");
            return;
        }

        gameManager.playerObject.transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
    }
}
