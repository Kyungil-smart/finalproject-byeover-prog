// 담당자 : 정승우
// 설명   : 튜토리얼 단계 목록 데이터(ScriptableObject).
//          순서/씬/완료조건 = 정승우(두뇌), 강조대상/말풍선 텍스트 = 홍정옥(UI)이 인스펙터에서 채운다.
//          Assets에서 우클릭 > Create > AprilLog > Tutorial Step Data 로 생성.

using System.Collections.Generic;
using UnityEngine;

/// <summary>튜토리얼 단계가 일어나는 씬.</summary>
public enum TutorialScene
{
    Lobby,
    InGame,
}

/// <summary>이 단계를 어떻게 넘기는가.</summary>
public enum TutorialAdvanceMode
{
    TapHighlight,   // 강조된 영역을 탭하면 다음 단계 (TutorialView가 처리)
    GameAction,     // 정렬/인챈트 선택 등 실제 게임 동작이 일어나면 다음 (게임 이벤트 훅이 TutorialManager.AdvanceStep 호출)
}

/// <summary>GameAction 단계가 무엇으로 진행되는가.</summary>
public enum TutorialGameAction
{
    None,
    Sort,           // 정렬 완성
    ArtifactEquip,  // 아티팩트 장착
    ArtifactOpen,   // 아티팩트 상세창 열림
}

/// <summary>튜토리얼 한 단계의 정의.</summary>
[System.Serializable]
public class TutorialStep
{
    [Tooltip("단계 순서(0부터). 목록 인덱스와 별개로 식별/디버그용.")]
    public int stepId;

    [Tooltip("이 단계가 일어나는 씬")]
    public TutorialScene scene = TutorialScene.Lobby;

    [Tooltip("넘기는 방식: 탭 / 게임동작")]
    public TutorialAdvanceMode advanceMode = TutorialAdvanceMode.TapHighlight;

    [Tooltip("GameAction일 때 무엇으로 진행하는지. TapHighlight면 무시")]
    public TutorialGameAction gameAction = TutorialGameAction.None;

    [Tooltip("딤 없이 손가락만 표시. 팝업 내용을 가리면 안 되는 단계에 사용")]
    public bool noDim = false;

    [Header("UI (홍정옥 작성)")]
    [Tooltip("강조할 UI 요소 식별자. 씬의 TutorialView가 이 id로 대상을 찾아 강조한다.")]
    public string highlightTargetId;

    [Tooltip("말풍선에 보여줄 안내 문구 (예: '여기를 눌러보세요')")]
    [TextArea] public string guideText;

    [Header("메모 (기획/개발)")]
    [Tooltip("이 단계가 무엇인지/무엇으로 넘어가는지 설명. 동작 안 함, 참고용.")]
    [TextArea] public string note;
}

[CreateAssetMenu(fileName = "TutorialStepData", menuName = "AprilLog/Tutorial Step Data")]
public class TutorialStepData : ScriptableObject
{
    [SerializeField] private List<TutorialStep> _steps = new List<TutorialStep>();

    public IReadOnlyList<TutorialStep> Steps => _steps;
    public int Count => _steps.Count;

    public TutorialStep Get(int index)
    {
        if (index < 0 || index >= _steps.Count) return null;
        return _steps[index];
    }
}
