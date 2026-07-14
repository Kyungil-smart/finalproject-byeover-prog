#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;

// 로비에서 테스트용 재화를 즉시 지급하는 콘솔. 레벨업/강화/돌파 등을 재화 걱정 없이 확인할 때 사용.
// _Lobby 씬 로드 시 스스로 생성되므로 씬 배선이 필요 없다. 에디터/개발 빌드에서만 컴파일된다.
public class TestCurrencyConsole : MonoBehaviour
{
    // 로비 + 아티팩트 백엔드 테스트 씬. GameManager 없는 테스트 씬에서도 강화석/조각을 넣을 수 있어야 한다.
    private static readonly string[] AllowedScenes = { "_Lobby", "_InGameTest", "_Test" };

    // 재화 아이템 ID (item_master 기준)
    private const int GoldId = 70001;
    private const int ParchmentId = 70002;
    private const int DiamondId = 70003;
    private const int UpgradeStoneId = 70004;
    private const int LegendaryShardId = 70005;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryAttach(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => TryAttach(scene, mode);

    private static void TryAttach(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || System.Array.IndexOf(AllowedScenes, scene.name) < 0) return;
        if (FindFirstObjectByType<TestCurrencyConsole>() == null)
        {
            new GameObject("TestCurrencyConsole").AddComponent<TestCurrencyConsole>();
            Debug.Log($"[TestCurrencyConsole] 설치됨 (씬: {scene.name})");
        }
    }

    private bool _open;
    private string _status = "";
    private GUIStyle _panel, _header, _label, _button;
    private bool _stylesReady;

    private void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.width / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        EnsureStyles();

        // 상단 재화 UI와 겹치지 않게 화면 세로 중앙쯤 왼쪽에 배치
        float vh = Screen.height / scale;
        float btnY = vh * 0.5f;

        if (GUI.Button(new Rect(8f, btnY, 110f, 48f), _open ? "닫기" : "재화", _button))
            _open = !_open;

        if (!_open) return;

        GUILayout.BeginArea(new Rect(8f, btnY + 54f, 300f, 420f), _panel);
        GUILayout.Label("테스트 재화 지급", _header);

        DrawGrant("골드 +100,000", GoldId, 100000);
        DrawGrant("양피지 +10,000", ParchmentId, 10000);
        DrawGrant("다이아 +10,000", DiamondId, 10000);
        DrawGrant("강화석 +9,999", UpgradeStoneId, 9999);
        DrawGrant("조각 +999", LegendaryShardId, 999);

        GUILayout.Space(6f);
        if (GUILayout.Button("전부 지급", _button))
        {
            Grant(GoldId, 100000);
            Grant(ParchmentId, 10000);
            Grant(DiamondId, 10000);
            Grant(UpgradeStoneId, 9999);
            Grant(LegendaryShardId, 999);
            _status = "전부 지급 완료";
        }

        if (!string.IsNullOrEmpty(_status))
            GUILayout.Label(_status, _label);

        GUILayout.EndArea();
    }

    private void DrawGrant(string caption, int itemId, int amount)
    {
        if (GUILayout.Button(caption, _button))
        {
            Grant(itemId, amount);
            _status = $"{caption} 지급";
        }
    }

    private void Grant(int itemId, int amount)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(itemId, amount, "테스트 지급");
            return;
        }

        // GameManager 없는 테스트 씬: 강화석/조각은 ArtifactManager의 로컬 재화로 넣는다(레벨업이 읽는 그 값).
        ArtifactManager artifact = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
        if (artifact != null && itemId == UpgradeStoneId)
        {
            artifact.AddStone(amount);
            return;
        }
        if (artifact != null && itemId == LegendaryShardId)
        {
            artifact.AddShard(amount);
            return;
        }

        _status = "GameManager 없음: 골드/양피지/다이아는 부트 씬 경유 필요";
    }

    private void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };
        _header = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
        _label = new GUIStyle(GUI.skin.label) { fontSize = 15 };
        _button = new GUIStyle(GUI.skin.button) { fontSize = 16, fixedHeight = 40f };
    }
}
#endif
