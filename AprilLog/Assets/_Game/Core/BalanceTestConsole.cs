// 담당자 : 정승우
// 설명   : _Test 씬 전용 밸런스 테스트 콘솔 (기획 요청: 시트 기반 스테이지별 난이도 체크)
//          - 원하는 스테이지 바로 진입 (챕터/스테이지 버튼, 튜토리얼 제외)
//          - 성장 레벨 시뮬레이션 (다음 진입부터 OutGrowthBonus에 반영)
//          - 인게임 수치 즉시 조절 (공격력/체력/공격속도) + 배속
//
// 사용법 : 에디터에서 Assets/Scenes/_Test.unity 를 열고 재생 -> 우상단 [TEST] 버튼.
//          시트 수치를 바꾼 경우 재파싱(Reimport) 후 다시 재생하면 반영된다.
//
// _Test 씬이 로드되면 스스로 생성되므로 씬 배선이 필요 없다(_Test는 _InGame 사본이라 씬 수술 금지).
// 다른 씬으로 나가면 정적 오버라이드를 전부 초기화해 일반 플레이에는 영향을 주지 않는다.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BalanceTestConsole : MonoBehaviour
{
    private const string TestSceneName = "_Test";

    /// <summary>시작 스테이지 오버라이드. 풀 Stage_ID(Chapter_ID*100+순서) — GameManager.SelectedChapterId와 같은 계약. 0 = 미사용.</summary>
    public static int PendingStageId { get; private set; }

    /// <summary>아웃게임 성장 레벨 오버라이드. InGameBootstrap.GetCharacterLevel이 최우선으로 읽는다. 0 = 계정 레벨 그대로.</summary>
    public static int OverrideCharacterLevel { get; private set; }

    // ---------- 자동 설치 ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryAttach(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAttach(scene, mode);
    }

    private static void TryAttach(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;   // 애디티브 로드는 씬 전환이 아니므로 무시

        if (scene.name != TestSceneName)
        {
            // 테스트 씬 밖으로 나가면 오버라이드를 비워 일반 플레이 오염을 막는다.
            PendingStageId = 0;
            OverrideCharacterLevel = 0;
            return;
        }

        if (FindFirstObjectByType<BalanceTestConsole>() == null)
            new GameObject("BalanceTestConsole").AddComponent<BalanceTestConsole>();
    }

    // ---------- 상태 ----------
    private static bool _open;   // 스테이지 재진입(씬 리로드) 후에도 패널 열림 상태 유지
    private Vector2 _stageScroll;
    private PlayerModel _player;
    private List<int> _chapterIds;
    private string _status = "";
    private float _speed = 1f;

    private GUIStyle _panel, _header, _label, _button;
    private bool _stylesReady;

    // ---------- GUI ----------
    private void OnGUI()
    {
        // 해상도와 무관하게 가로 720 기준 가상 좌표로 그린다 (폰/태블릿/에디터 공통 크기)
        float scale = Mathf.Max(1f, Screen.width / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float vw = Screen.width / scale;
        float vh = Screen.height / scale;

        EnsureStyles();

        if (GUI.Button(new Rect(vw - 96f, 8f, 88f, 44f), _open ? "닫기" : "TEST", _button))
            _open = !_open;

        if (!_open) return;

        if (_player == null) _player = FindFirstObjectByType<PlayerModel>();

        GUILayout.BeginArea(new Rect(vw - 436f, 60f, 428f, vh - 80f), _panel);

        DrawStatus();
        GUILayout.Space(6f);
        DrawLevelSimulation();
        GUILayout.Space(6f);
        DrawStageSelect();
        GUILayout.Space(6f);
        DrawLiveTuning();
        GUILayout.Space(6f);
        DrawSpeed();

        if (!string.IsNullOrEmpty(_status))
            GUILayout.Label(_status, _label);

        GUILayout.EndArea();
    }

    private void DrawStatus()
    {
        GUILayout.Label("밸런스 테스트 콘솔", _header);

        string stage = PendingStageId > 0
            ? $"{PendingStageId / 100}챕터 {PendingStageId % 100}스테이지"
            : "기본 진입";
        GUILayout.Label($"선택 스테이지: {stage}", _label);

        if (_player != null)
            GUILayout.Label($"공격력 {_player.Attack} / HP {_player.CurrentHP}/{_player.MaxHP} / 공속간격 {_player.AttackSpeed:F2}s", _label);
        GUILayout.Label($"배속 x{_speed:F1}   성장레벨 {(OverrideCharacterLevel > 0 ? OverrideCharacterLevel.ToString() : "계정값")}", _label);
    }

    private void DrawLevelSimulation()
    {
        GUILayout.Label("성장 레벨 시뮬 (다음 스테이지 진입부터 반영)", _header);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", _button)) BumpLevel(-10);
        if (GUILayout.Button("-1", _button)) BumpLevel(-1);
        GUILayout.Label(OverrideCharacterLevel > 0 ? $"Lv {OverrideCharacterLevel}" : "계정값", _label, GUILayout.Width(80f));
        if (GUILayout.Button("+1", _button)) BumpLevel(1);
        if (GUILayout.Button("+10", _button)) BumpLevel(10);
        if (GUILayout.Button("해제", _button)) OverrideCharacterLevel = 0;
        GUILayout.EndHorizontal();
    }

    private void BumpLevel(int delta)
    {
        int baseLevel = OverrideCharacterLevel > 0 ? OverrideCharacterLevel : 1;
        OverrideCharacterLevel = Mathf.Clamp(baseLevel + delta, 1, 999);
    }

    private void DrawStageSelect()
    {
        GUILayout.Label("스테이지 진입 (진입 시 새 판으로 시작)", _header);

        var repo = DataManager.Instance != null ? DataManager.Instance.StageRepo : null;
        var chapters = GetChapterIds(repo);
        if (repo == null || chapters.Count == 0)
        {
            GUILayout.Label("StageRepo가 아직 초기화되지 않았습니다.", _label);
            return;
        }

        _stageScroll = GUILayout.BeginScrollView(_stageScroll, GUILayout.Height(330f));
        foreach (int chapterId in chapters)
        {
            var chapter = repo.GetChapter(chapterId);
            if (chapter == null) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{chapterId / 100}-{chapter.ChapterOrder}", _label, GUILayout.Width(56f));
            for (int order = 1; order <= chapter.StageCount; order++)
            {
                if (GUILayout.Button(order.ToString(), _button, GUILayout.Width(44f)))
                    EnterStage(chapterId, order);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        if (PendingStageId > 0 && GUILayout.Button("현재 스테이지 재시작", _button))
            EnterStage(PendingStageId / 100, PendingStageId % 100);
    }

    private List<int> GetChapterIds(StageRepo repo)
    {
        if (_chapterIds != null && _chapterIds.Count > 0) return _chapterIds;

        _chapterIds = new List<int>();
        if (repo == null) return _chapterIds;

        // 진행 순서 인덱스 -> Chapter_ID (StageRepo가 튜토리얼 챕터는 미리 제외해 둔다)
        var map = repo.GetStepIndexToChapterIdMappingData();
        for (int i = 0; i < map.Count; i++)
        {
            if (map.TryGetValue(i, out int id))
                _chapterIds.Add(id);
        }
        return _chapterIds;
    }

    private void EnterStage(int chapterId, int stageOrder)
    {
        var repo = DataManager.Instance != null ? DataManager.Instance.StageRepo : null;
        if (repo == null || repo.GetStageId(chapterId, stageOrder) <= 0)
        {
            _status = $"데이터에 없는 스테이지: {chapterId}-{stageOrder}";
            return;
        }

        PendingStageId = chapterId * 100 + stageOrder;

        if (GameManager.Instance != null)
        {
            // 이어하기 세이브가 선택을 덮지 않도록 지우고, 정식 계약(SelectedChapterId)에도 같이 반영한다.
            GameManager.Instance.DeleteLocalSave();
            GameManager.Instance.SelectedChapterId = PendingStageId;
        }

        Time.timeScale = 1f;
        _speed = 1f;
        SceneManager.LoadScene(TestSceneName);
    }

    private void DrawLiveTuning()
    {
        GUILayout.Label("수치 조절 (현재 판에 즉시 적용)", _header);

        if (_player == null)
        {
            GUILayout.Label("PlayerModel을 찾는 중...", _label);
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("공격력", _label, GUILayout.Width(56f));
        if (GUILayout.Button("-100", _button)) _player.StatusEnhance(PlayerStatus.Attack, CalFormula.Add, 100f, true);
        if (GUILayout.Button("-10", _button)) _player.StatusEnhance(PlayerStatus.Attack, CalFormula.Add, 10f, true);
        if (GUILayout.Button("+10", _button)) _player.StatusEnhance(PlayerStatus.Attack, CalFormula.Add, 10f, false);
        if (GUILayout.Button("+100", _button)) _player.StatusEnhance(PlayerStatus.Attack, CalFormula.Add, 100f, false);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("체력", _label, GUILayout.Width(56f));
        if (GUILayout.Button("-500", _button)) _player.StatusEnhance(PlayerStatus.Hp, CalFormula.Add, 500f, true);
        if (GUILayout.Button("-100", _button)) _player.StatusEnhance(PlayerStatus.Hp, CalFormula.Add, 100f, true);
        if (GUILayout.Button("+100", _button)) _player.StatusEnhance(PlayerStatus.Hp, CalFormula.Add, 100f, false);
        if (GUILayout.Button("+500", _button)) _player.StatusEnhance(PlayerStatus.Hp, CalFormula.Add, 500f, false);
        if (GUILayout.Button("회복", _button)) _player.Heal(_player.MaxHP);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("공속", _label, GUILayout.Width(56f));
        // AttackSpeed는 공격 간격(초): 빠르게 = 간격 감소
        if (GUILayout.Button("느리게 +0.1s", _button)) _player.StatusEnhance(PlayerStatus.AttackSpeed, CalFormula.Add, 0.1f, true);
        if (GUILayout.Button("빠르게 -0.1s", _button)) _player.StatusEnhance(PlayerStatus.AttackSpeed, CalFormula.Add, 0.1f, false);
        GUILayout.EndHorizontal();
    }

    private void DrawSpeed()
    {
        GUILayout.Label("배속 (팝업 정지 중에는 잠김)", _header);

        GUI.enabled = !ScreenNavigator.IsMenuOpen;
        GUILayout.BeginHorizontal();
        foreach (float speed in new[] { 0.5f, 1f, 2f, 3f })
        {
            if (GUILayout.Button($"x{speed:F1}", _button))
            {
                _speed = speed;
                Time.timeScale = speed;
            }
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;
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
