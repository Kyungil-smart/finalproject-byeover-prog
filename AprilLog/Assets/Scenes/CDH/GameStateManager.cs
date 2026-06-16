using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    public ArtifactManager ArtifactManager;

    private void Awake()
    {
        Instance = this;
        ArtifactManager.Initialize();
    }
}
