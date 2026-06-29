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
// 완료 플래그는 지금 PlayerPrefs(기기 단위). 계정 단위로 가려면 GameManager.CloudData에 필드 추가 후
// IsCompleted(getter)/Complete() 두 곳만 교체하면 됨(조규민 도메인이라 우선 PlayerPrefs).

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TutorialStepData _stepData;

#if UNITY_EDITOR
    [Tooltip("0 이상이면 Start에서 그 단계부터 시작한다(에디터 테스트용).")]
    [SerializeField] private int _debugStartIndex = -1;
#endif

    private const string DONE_KEY = "Tutorial_Completed";

    private int _currentIndex = -1;     // -1 = 진행 중 아님
    private ITutorialView _view;        // 현재 씬의 오버레이(있을 때만)

    public bool IsCompleted => PlayerPrefs.GetInt(DONE_KEY, 0) == 1;
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
        PlayerPrefs.SetInt(DONE_KEY, 1);
        PlayerPrefs.Save();
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
