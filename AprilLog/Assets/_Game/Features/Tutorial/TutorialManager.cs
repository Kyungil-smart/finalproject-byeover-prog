// 담당자 : 정승우
// 설명   : 튜토리얼 "두뇌". 진행 단계를 씬 전환에도 유지하고, 한 번 끝내면 다시 안 뜨게 저장한다.
//          UI 표현(어두운 막/강조/말풍선)은 각 씬의 TutorialView(홍정옥)가 담당. 이 매니저는 진행 로직만.
//
// 흐름:
//   새 게임 시작 → TryStart() → 0단계부터.
//   각 씬 로드 → 그 씬의 TutorialView가 RegisterView(this)로 자기를 등록 → 현재 단계가 이 씬이면 그려줌.
//   유저가 동작 완료 → (탭: View가 OnStepActionCompleted 발행 / 게임동작: 게임 이벤트 훅이 AdvanceStep 호출) → 다음 단계.
//   마지막 단계 통과 → Complete() → PlayerPrefs에 완료 저장 → 이후 TryStart 무시.
//
// 완료 플래그는 로그인 이후 계정 데이터에 저장하고, GameManager가 없을 때만 PlayerPrefs를 대체 저장소로 사용한다.
//
// 1차 수정자 : 조규민
// 수정 내용 : 로그인 계정의 클라우드 데이터에서 튜토리얼 완료 상태를 판정하고 완료 즉시 저장

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TutorialStepData _stepData;

    // 직렬화 필드는 항상 컴파일해야 에디터/빌드 레이아웃이 일치한다(사용만 에디터 전용).
    [Tooltip("0 이상이면 Start에서 그 단계부터 시작한다(에디터 테스트용).")]
    [SerializeField] private int _debugStartIndex = -1;

    private const string DONE_KEY = "Tutorial_Completed";
    private const string IN_GAME_SCENE_NAME = "_InGame";
    private const string IN_GAME_OVERLAY_RESOURCE = "Tutorial/TutorialInGameOverlay";
    private const string LOBBY_SCENE_NAME = "_Lobby";

    private int _currentIndex = -1;     // -1 = 진행 중 아님
    private ITutorialView _view;        // 현재 씬의 오버레이(있을 때만)
    private GameObject _spawnedInGameOverlay;
    private string _previousSceneName;  // 직전 로드된 씬 이름(전투 복귀 판별용)

    public bool IsCompleted => GameManager.Instance != null
        ? GameManager.Instance.IsTutorialCompleted()
        : PlayerPrefs.GetInt(DONE_KEY, 0) == 1;
    public bool IsRunning => _currentIndex >= 0;
    public TutorialStep CurrentStep => _stepData != null ? _stepData.Get(_currentIndex) : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

#if UNITY_EDITOR
    private void Start()
    {
        if (_debugStartIndex >= 0) StartAt(_debugStartIndex);
    }
#endif

    // ---------- 시작/진행 ----------

    /// <summary>새 게임 진입 시 호출. 이미 완료했거나 데이터 없으면 아무것도 안 함.</summary>
    public void TryStart()
    {
        if (IsCompleted) { Debug.Log("[Tutorial] 이미 완료됨 — 실행 안 함."); return; }
        if (IsRunning) { RefreshView(); return; }   // 진행 중이면 0단계로 리셋하지 말고 현재 단계만 다시 표시
        if (_stepData == null || _stepData.Count == 0)
        {
            Debug.LogWarning("[Tutorial] StepData가 비어 있어 시작하지 못함. 인스펙터에 TutorialStepData 연결 필요.");
            return;
        }
        _currentIndex = 0;
        Debug.Log("[Tutorial] 시작 (0단계)");
        RefreshView();
    }

    /// <summary>각 씬의 TutorialView가 로드되면 자기를 등록한다(중복 구독 방지 포함).</summary>
    public void RegisterView(ITutorialView view)
    {
        if (_view != null) _view.OnStepActionCompleted -= AdvanceStep;
        _view = view;
        if (_view != null) _view.OnStepActionCompleted += AdvanceStep;
        RefreshView();
    }

    /// <summary>씬이 바뀌어 View가 사라질 때 해제(선택).</summary>
    public void UnregisterView(ITutorialView view)
    {
        if (_view != view) return;
        _view.OnStepActionCompleted -= AdvanceStep;
        _view = null;
    }

    /// <summary>다음 단계로. 탭 또는 게임동작 완료 시 호출된다. 마지막이면 완료 처리.</summary>
    public void AdvanceStep()
    {
        if (!IsRunning) return;
        _currentIndex++;
        if (_stepData == null || _currentIndex >= _stepData.Count)
        {
            Complete();
            return;
        }
        Debug.Log($"[Tutorial] {_currentIndex}단계로 진행");
        RefreshView();
    }

    /// <summary>튜토리얼 완료 — 다시 안 뜨게 저장하고 오버레이 숨김.</summary>
    public void Complete()
    {
        _currentIndex = -1;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MarkTutorialCompleted();
        }
        else
        {
            PlayerPrefs.SetInt(DONE_KEY, 1);
            PlayerPrefs.Save();
        }

        if (_view != null) _view.Hide();
        Debug.Log("[Tutorial] 완료 — 저장됨(다시 안 뜸).");
    }

    // ---------- 내부 ----------

    // 현재 단계가 '지금 켜진 씬'의 것이면 그려주고, 아니면 숨긴다.
    // (씬 전환은 게임 본 흐름이 담당 — 튜토리얼은 자연스러운 흐름 안에서 해당 씬 단계만 노출)
    private void RefreshView()
    {
        if (_view == null) return;

        TutorialStep step = CurrentStep;
        if (step == null) { _view.Hide(); return; }

        if (!IsStepForActiveScene(step)) { _view.Hide(); return; }

        _view.ShowStep(step);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬별 튜토리얼 오버레이를 진행 중일 때만 스폰한다. 이전 씬 오버레이는 씬 언로드로 이미 파괴됐으니 참조만 정리.
        // 로비 오버레이(--- Tutorial ---)는 _Lobby 씬에 프리팹 인스턴스로 직접 배치되어 있어 여기서 스폰하지 않는다.
        if (scene.name == IN_GAME_SCENE_NAME)
        {
            TrySpawnOverlay(IN_GAME_OVERLAY_RESOURCE, ref _spawnedInGameOverlay);
        }
        else
        {
            _spawnedInGameOverlay = null;
        }

        HealStaleInGameStepOnLobbyReturn(scene.name);
        _previousSceneName = scene.name;
    }

    // 인게임 전투에서 로비로 돌아왔는데 튜토리얼 단계가 아직 InGame 단계에 멈춰 있으면
    // 다음 로비 단계까지 이어 붙인다. (예: 방치 사망이 범람 패배 흐름을 타지 못해 단계 전진이 누락된 경우)
    // 최초 튜토리얼은 로비에서 InGame 단계로 시작해 인게임으로 넘어가므로, 그 첫 전투를 건너뛰지 않도록
    // 직전 씬이 인게임이었을 때(=전투에서 복귀한 경우)만 보정한다.
    private void HealStaleInGameStepOnLobbyReturn(string loadedSceneName)
    {
        if (loadedSceneName != LOBBY_SCENE_NAME) return;
        if (_previousSceneName != IN_GAME_SCENE_NAME) return;
        if (!IsRunning) return;

        // 첫 전투(방치 사망이 문제되는 stepId 0~3)에 한정한다. 후반 재진입(step14) 실패 복귀까지
        // 이어붙이면 튜토리얼이 조기 완료될 수 있어 제외한다.
        TutorialStep step = CurrentStep;
        if (step == null || step.scene != TutorialScene.InGame || step.stepId > 3) return;

        Debug.LogWarning("[Tutorial] 전투 복귀 후 첫 전투 InGame 단계가 남아 있어 다음 로비 단계로 이어붙입니다.");
        while (IsRunning && CurrentStep != null && CurrentStep.scene == TutorialScene.InGame && CurrentStep.stepId <= 3)
        {
            AdvanceStep();
        }
    }

    private void TrySpawnOverlay(string resourcePath, ref GameObject spawned)
    {
        if (!IsRunning || spawned != null) return;

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[Tutorial] 튜토리얼 오버레이 프리팹을 찾지 못했습니다: Resources/{resourcePath}");
            return;
        }

        spawned = Instantiate(prefab);
        spawned.name = prefab.name;
    }

    private bool IsStepForActiveScene(TutorialStep step)
    {
        string active = SceneManager.GetActiveScene().name;
        switch (step.scene)
        {
            case TutorialScene.Lobby:  return active == "_Lobby";
            case TutorialScene.InGame: return active == "_InGame";
            default: return false;
        }
    }

    // ---------- 테스트/디버그 ----------

    /// <summary>지정 단계부터 시작한다(테스트/디버그용).</summary>
    public void StartAt(int index)
    {
        if (_stepData == null || index < 0 || index >= _stepData.Count) return;
        _currentIndex = index;
        Debug.Log($"[Tutorial] 디버그 시작: {index}단계");
        RefreshView();
    }

    /// <summary>완료 플래그 초기화(테스트용). 다음 새 게임에서 튜토리얼 다시 실행됨.</summary>
    [ContextMenu("Reset Tutorial Flag")]
    public void ResetTutorialFlag()
    {
        PlayerPrefs.DeleteKey(DONE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] 완료 플래그 초기화됨.");
    }
}
