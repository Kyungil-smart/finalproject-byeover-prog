// 진행 단계에 따라 로비 UI를 단계적으로 노출/잠금한다.
// 0: 전투 진입만
// 1: 하단바 + 성장·전투
// 2: 상점 해제
// 3: 일퀘·출첵 노출

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialLobbyGate : MonoBehaviour
{
    [Serializable]
    public class GatedElement
    {
        [Tooltip("구분용 이름")]
        public string label;

        [Tooltip("이 단계 이상이면 해제 또는 노출됩니다.")]
        public int unlockPhase = 1;

        [Header("처리 대상")]
        [Tooltip("잠금 상태에서는 숨기고, 해제되면 표시할 UI입니다.")]
        public GameObject showHideTarget;

        [Tooltip("화면에는 표시하되 잠금 상태에서 입력을 막을 Selectable입니다.")]
        public Selectable lockTarget;

        [Tooltip("잠금 상태에서 스프라이트를 교체할 이미지입니다.")]
        public Image iconTarget;

        [Tooltip("잠금 상태에 사용할 스프라이트입니다.")]
        public Sprite lockedSprite;

        [NonSerialized]
        public Sprite normalSprite;

        [NonSerialized]
        public bool cached;
    }

    [Header("단계별 게이트 대상")]
    [SerializeField]
    private List<GatedElement> _elements = new();

    [Header("시작 단계 테스트 설정")]
    [Tooltip("외부에서 단계가 적용되지 않았을 때만 사용할 테스트용 시작 단계입니다.")]
    [SerializeField]
    private int _startPhase;

    [Tooltip("TutorialView가 단계를 적용하지 않은 경우에만 시작 단계를 적용합니다.")]
    [SerializeField]
    private bool _applyOnStart = true;

    [Header("디버그")]
    [SerializeField]
    private bool _showDebugLog = true;

    public int CurrentPhase { get; private set; }

    /// <summary>
    /// TutorialView 등 외부 시스템에서 한 번이라도 단계가 적용되었는지 여부입니다.
    /// </summary>
    public bool HasExternalPhaseApplied { get; private set; }

    private void Awake()
    {
        CacheNormalSprites();
    }

    private void Start()
    {
        /*
         * TutorialView.Start가 먼저 실행된 경우:
         * 이미 SetPhase가 호출됐으므로 테스트용 _startPhase로 덮어쓰면 안 됩니다.
         *
         * TutorialLobbyGate.Start가 먼저 실행된 경우:
         * 일단 _startPhase를 적용하고 이후 TutorialView가 실제 phase를 덮어씁니다.
         */
        if (!_applyOnStart)
        {
            LogDebug("시작 단계 자동 적용 비활성화.");
            return;
        }

        if (HasExternalPhaseApplied)
        {
            LogDebug(
                $"시작 단계 적용 생략. " +
                $"외부에서 이미 Phase={CurrentPhase}가 적용되었습니다.");

            return;
        }

        ApplyPhaseInternal(_startPhase, false);

        LogDebug(
            $"테스트용 시작 단계 적용. Phase={_startPhase}");
    }

    /// <summary>
    /// TutorialView 또는 다른 외부 시스템에서 진행 단계를 적용합니다.
    /// </summary>
    public void SetPhase(int phase)
    {
        HasExternalPhaseApplied = true;
        ApplyPhaseInternal(phase, true);
    }

    /// <summary>
    /// 모든 게이트 요소를 해금합니다(튜토리얼 완료/스킵 시).
    /// </summary>
    public void UnlockAll()
    {
        int max = 0;
        if (_elements != null)
        {
            foreach (GatedElement element in _elements)
            {
                if (element != null && element.unlockPhase > max)
                {
                    max = element.unlockPhase;
                }
            }
        }

        SetPhase(max);
    }

    /// <summary>
    /// 내부에서 실제 단계 상태를 적용합니다.
    /// </summary>
    private void ApplyPhaseInternal(int phase, bool externalRequest)
    {
        CurrentPhase = phase;

        if (_elements == null)
        {
            Debug.LogWarning(
                "[TutorialLobbyGate] Elements 리스트가 null입니다.",
                this);

            return;
        }

        LogDebug(
            $"단계 적용 시작. " +
            $"Phase={phase}, " +
            $"External={externalRequest}, " +
            $"ElementCount={_elements.Count}");

        for (int i = 0; i < _elements.Count; i++)
        {
            ApplyElement(_elements[i], phase, i);
        }

        LogDebug(
            $"단계 적용 완료. Phase={phase}");
    }

    private void ApplyElement(
        GatedElement element,
        int phase,
        int index)
    {
        if (element == null)
        {
            return;
        }

        bool unlocked = phase >= element.unlockPhase;

        if (element.showHideTarget != null)
        {
            element.showHideTarget.SetActive(unlocked);
        }

        if (element.lockTarget != null)
        {
            element.lockTarget.interactable = unlocked;
        }

        if (element.iconTarget != null &&
            element.cached &&
            Application.isPlaying)
        {
            element.iconTarget.sprite =
                unlocked || element.lockedSprite == null
                    ? element.normalSprite
                    : element.lockedSprite;
        }

        LogDebug(
            $"요소 적용. " +
            $"Index={index}, " +
            $"Label={element.label}, " +
            $"UnlockPhase={element.unlockPhase}, " +
            $"CurrentPhase={phase}, " +
            $"Unlocked={unlocked}, " +
            $"ShowHideTarget=" +
            $"{(element.showHideTarget != null ? element.showHideTarget.name : "NULL")}, " +
            $"LockTarget=" +
            $"{(element.lockTarget != null ? element.lockTarget.name : "NULL")}, " +
            $"Interactable=" +
            $"{(element.lockTarget != null ? element.lockTarget.interactable.ToString() : "N/A")}");
    }

    private void CacheNormalSprites()
    {
        if (_elements == null)
        {
            return;
        }

        foreach (GatedElement element in _elements)
        {
            if (element == null || element.iconTarget == null)
            {
                continue;
            }

            element.normalSprite = element.iconTarget.sprite;
            element.cached = true;
        }
    }

    private void LogDebug(string message)
    {
        if (!_showDebugLog)
        {
            return;
        }

        Debug.Log(
            $"[TutorialLobbyGate] {message}",
            this);
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        CacheNormalSprites();

        if (!_applyOnStart)
        {
            return;
        }

        if (_elements == null)
        {
            return;
        }

        foreach (GatedElement element in _elements)
        {
            ApplyElementInEditor(element, _startPhase);
        }
    }

    private void ApplyElementInEditor(
        GatedElement element,
        int phase)
    {
        if (element == null)
        {
            return;
        }

        bool unlocked = phase >= element.unlockPhase;

        if (element.showHideTarget != null)
        {
            element.showHideTarget.SetActive(unlocked);
        }

        if (element.lockTarget != null)
        {
            element.lockTarget.interactable = unlocked;
        }
    }

    [ContextMenu("단계 0 적용")]
    private void ApplyPhase0()
    {
        SetPhase(0);
    }

    [ContextMenu("단계 1 적용")]
    private void ApplyPhase1()
    {
        SetPhase(1);
    }

    [ContextMenu("단계 2 적용")]
    private void ApplyPhase2()
    {
        SetPhase(2);
    }

    [ContextMenu("단계 3 적용")]
    private void ApplyPhase3()
    {
        SetPhase(3);
    }

    [ContextMenu("외부 적용 상태 초기화")]
    private void ResetExternalPhaseState()
    {
        HasExternalPhaseApplied = false;

        Debug.Log(
            "[TutorialLobbyGate] 외부 단계 적용 상태를 초기화했습니다.",
            this);
    }

#endif
}