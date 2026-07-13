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

        // Inspector에는 ArtifactManager 프리팹 에셋이 연결되어 있으므로 런타임 인스턴스를 만든다.
        // 에셋 참조를 그대로 사용하면 MonoBehaviour.Start가 실행되지 않아 로드와 저장 이벤트 구독이 누락된다.
        if (ArtifactManager != null && !ArtifactManager.gameObject.scene.IsValid())
        {
            ArtifactManager = Instantiate(ArtifactManager, transform);
        }

        if (ArtifactManager == null)
        {
            Debug.LogError("[GameStateManager] ArtifactManager가 연결되지 않았습니다.", this);
            return;
        }

        ArtifactManager.Initialize();
    }
}

