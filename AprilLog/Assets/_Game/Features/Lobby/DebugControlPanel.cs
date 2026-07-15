#if APRILOG_DEBUG
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 빌드에서 게임 전체를 버튼으로 제어하는 디버그 패널. 계정 없이 팀원이 테스트할 수 있게 만든다.
// APRILOG_DEBUG 심볼이 있을 때만 컴파일된다(Player Settings > Scripting Define Symbols).
// 스토어 제출 빌드에서는 이 심볼을 빼면 코드 전체가 사라진다.
// 씬 로드 시 스스로 생성되므로 씬 배선이 필요 없다.
public class DebugControlPanel : MonoBehaviour
{
    private static readonly string[] AllowedScenes = { "_Lobby", "_InGame", "_InGameTest", "_Test" };

    // 재화 아이템 ID (item_master 기준)
    private const int GoldId = 70001;
    private const int ParchmentId = 70002;
    private const int DiamondId = 70003;
    private const int UpgradeStoneId = 70004;
    private const int LegendaryShardId = 70005;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded += (scene, mode) => TryAttach(scene, mode);
        TryAttach(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void TryAttach(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || System.Array.IndexOf(AllowedScenes, scene.name) < 0) return;
        if (FindFirstObjectByType<DebugControlPanel>() == null)
            new GameObject("DebugControlPanel").AddComponent<DebugControlPanel>();
    }

    private bool _open;
    private Vector2 _scroll;
    private string _status = "";

    // 스테이지 선택 입력값
    private int _chapterIndex;
    private int _stageOrder = 1;

    private GUIStyle _panel, _header, _label, _button;
    private bool _stylesReady;

    private void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.width / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        EnsureStyles();

        float vh = Screen.height / scale;
        float btnY = vh * 0.4f;

        if (GUI.Button(new Rect(8f, btnY, 120f, 48f), _open ? "닫기" : "디버그", _button))
            _open = !_open;

        if (!_open) return;

        GUILayout.BeginArea(new Rect(8f, btnY + 54f, 340f, vh - btnY - 70f), _panel);
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label("재화 지급", _header);
        DrawGrant("골드 +100,000", GoldId, 100000);
        DrawGrant("양피지 +10,000", ParchmentId, 10000);
        DrawGrant("다이아 +10,000", DiamondId, 10000);
        DrawGrant("강화석 +9,999", UpgradeStoneId, 9999);
        DrawGrant("조각 +999", LegendaryShardId, 999);
        if (GUILayout.Button("전부 지급", _button)) GrantAll();
        if (GUILayout.Button("조커블록 +1", _button)) GrantJoker();
        if (GUILayout.Button("조커블록 쿨타임 초기화", _button)) ResetJokerCooldown();

        GUILayout.Space(10f);
        GUILayout.Label("스테이지", _header);
        DrawIntRow("챕터 인덱스", ref _chapterIndex, 0);
        DrawIntRow("스테이지 순서", ref _stageOrder, 1);
        if (GUILayout.Button("이 스테이지 해금+저장", _button)) SetStageProgress();
        if (GUILayout.Button("이 챕터 인게임 진입", _button)) EnterStage();

        GUILayout.Space(10f);
        GUILayout.Label($"배속 (현재 x{Time.timeScale:0.#})", _header);
        GUILayout.BeginHorizontal();
        DrawSpeed("x1", 1f);
        DrawSpeed("x2", 2f);
        DrawSpeed("x4", 4f);
        DrawSpeed("x8", 8f);
        GUILayout.EndHorizontal();

        GUILayout.Space(10f);
        GUILayout.Label("초기화", _header);
        if (GUILayout.Button("세이브 전체 초기화", _button)) ResetSave();
        if (GUILayout.Button("튜토리얼 스킵", _button)) SkipTutorial();

        if (!string.IsNullOrEmpty(_status))
        {
            GUILayout.Space(6f);
            GUILayout.Label(_status, _label);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // ---------- 재화 ----------
    private void DrawGrant(string caption, int itemId, int amount)
    {
        if (GUILayout.Button(caption, _button))
        {
            Grant(itemId, amount);
            _status = $"{caption} 지급";
        }
    }

    // 인게임 QA 배속. 튜토리얼 연출이 timeScale 을 0↔1 로 바꾸면 그때 덮어써질 수 있다(QA 한정).
    private void DrawSpeed(string caption, float scale)
    {
        if (GUILayout.Button(caption, _button))
        {
            Time.timeScale = scale;
            _status = $"배속 x{scale:0.#}";
        }
    }

    // 쿨타임만 즉시 해제(보유 수는 유지). RestoreFromSave(잔여=0)로 마지막 사용시각을 초기화한다.
    // UI 카운트다운은 진행 중이던 연출이 끝날 때까지 잠깐 남을 수 있으나 조커는 바로 사용 가능하다.
    private void ResetJokerCooldown()
    {
        JokerSystem joker = FindFirstObjectByType<JokerSystem>();
        if (joker == null) { _status = "JokerSystem 없음(인게임 아님)"; return; }

        joker.RestoreFromSave(joker.GetJokerCount(), 0f);
        _status = "조커블록 쿨타임 초기화";
    }

    private void GrantJoker()
    {
        JokerSystem joker = FindFirstObjectByType<JokerSystem>();
        if (joker == null) { _status = "JokerSystem 없음(인게임 아님)"; return; }

        int before = joker.GetJokerCount();
        joker.AcquireJokerItem();
        int after = joker.GetJokerCount();
        _status = after > before ? $"조커블록 지급 (보유 {after})" : $"조커블록 최대치({after})";
    }

    private void GrantAll()
    {
        Grant(GoldId, 100000);
        Grant(ParchmentId, 10000);
        Grant(DiamondId, 10000);
        Grant(UpgradeStoneId, 9999);
        Grant(LegendaryShardId, 999);
        _status = "전부 지급 완료";
    }

    private void Grant(int itemId, int amount)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(itemId, amount, "디버그 지급");
            return;
        }

        // GameManager 없는 테스트 씬: 강화석/조각은 ArtifactManager 로컬 재화로 넣는다.
        ArtifactManager artifact = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
        if (artifact != null && itemId == UpgradeStoneId) { artifact.AddStone(amount); return; }
        if (artifact != null && itemId == LegendaryShardId) { artifact.AddShard(amount); return; }
        _status = "GameManager 없음: 골드/양피지/다이아는 부트 씬 경유 필요";
    }

    // ---------- 스테이지 ----------
    private int ResolveChapterId()
    {
        StageRepo repo = DataManager.Instance != null ? DataManager.Instance.StageRepo : null;
        if (repo == null) { _status = "StageRepo 없음"; return -1; }
        int chapterId = repo.GetChapterIdByIndex(_chapterIndex);
        if (chapterId == -1) _status = $"챕터 인덱스 {_chapterIndex} 없음";
        return chapterId;
    }

    private void SetStageProgress()
    {
        int chapterId = ResolveChapterId();
        if (chapterId == -1) return;

        int stageId = DataManager.Instance.StageRepo.GetStageId(chapterId, _stageOrder);
        if (stageId == -1) { _status = $"챕터 {chapterId} 스테이지 {_stageOrder} 없음"; return; }

        PlayerProgressModel progress = FindFirstObjectByType<PlayerProgressModel>();
        if (progress == null) { _status = "PlayerProgressModel 없음(로비 아님)"; return; }

        progress.UnlockStage(stageId);
        progress.SetCurrentStage(chapterId, stageId);
        if (GameManager.Instance != null) GameManager.Instance.SaveOutGameProgress(progress);
        _status = $"챕터 {chapterId} / 스테이지 {stageId} 해금·저장";
    }

    private void EnterStage()
    {
        int chapterId = ResolveChapterId();
        if (chapterId == -1) return;
        if (GameManager.Instance == null) { _status = "GameManager 없음"; return; }
        GameManager.Instance.LoadInGame(chapterId);
    }

    // ---------- 초기화 ----------
    private void ResetSave()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.CloudData == null) { _status = "CloudData 없음"; return; }

        UserCloudData cd = gm.CloudData;
        UserCloudData def = UserCloudData.CreateDefault();

        // 신원/설정은 유지하고 진행·재화·상태만 기본값으로 되돌린다.
        cd.gold = def.gold;
        cd.parchment = def.parchment;
        cd.diamond = def.diamond;
        cd.inventory = new List<ItemSaveEntry>();
        cd.staminaData = new List<StaminaSaveEntry>();
        cd.characterLevel = def.characterLevel;
        cd.currentChapter = def.currentChapter;
        cd.currentStage = def.currentStage;
        cd.unlockedStages = new List<int>(def.unlockedStages);
        cd.firstClearRewardedStages = new List<int>();
        cd.firstClearRewardedChapters = new List<int>();
        cd.firstReadScenarios = new List<int>();
        cd.pendingEnchantDraws = new List<EnchantDrawSnapshot>();
        cd.myArtifacts = new List<ArtifactInstance>();
        cd._hasInitialFlowState = true;
        cd._initialStoryStarted = false;
        cd._tutorialCompleted = false;

        PlayerPrefs.SetInt("Tutorial_Completed", 0);
        PlayerPrefs.SetInt("Tutorial_SkillExplainSeen", 0);   // 스킬 설명 재확인 가능하도록 초기화
        PlayerPrefs.Save();

        gm.SyncToCloud(cd);
        gm.LoadLobby();
        _status = "세이브 초기화 완료(로비 재진입)";
    }

    // 튜토리얼을 완료 처리(다시 안 뜨게 저장)하고 로비로 이동. 시나리오 스킵 버튼과 동일 동작.
    private void SkipTutorial()
    {
        if (TutorialManager.Instance != null) TutorialManager.Instance.Complete();

        GameManager gm = GameManager.Instance;
        if (gm != null) gm.LoadLobby();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("_Lobby");

        _status = "튜토리얼 스킵 완료(로비 재진입)";
    }

    // ---------- 입력/스타일 ----------
    private void DrawIntRow(string caption, ref int value, int min)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{caption}: {value}", _label, GUILayout.Width(180f));
        if (GUILayout.Button("-", _button, GUILayout.Width(46f))) value = Mathf.Max(min, value - 1);
        if (GUILayout.Button("+", _button, GUILayout.Width(46f))) value += 1;
        GUILayout.EndHorizontal();
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
