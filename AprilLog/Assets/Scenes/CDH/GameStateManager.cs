using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    public ArtifactManager ArtifactManager;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        ArtifactManager.Initialize();
    }
}

