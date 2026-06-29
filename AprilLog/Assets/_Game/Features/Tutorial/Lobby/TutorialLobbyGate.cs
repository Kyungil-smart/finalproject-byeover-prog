// 진행 단계에 따라 로비 UI를 단계적으로 노출/잠금한다.
//   0: 전투 진입만   1: 하단바+성장·전투   2: 상점 해제   3: 일퀘·출첵 노출

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialLobbyGate : MonoBehaviour
{
    // 각 UI 요소가 몇 단계에서 풀리는지 + 풀리기 전 어떻게 처리할지
    [Serializable]
    public class GatedElement
    {
        [Tooltip("구분용 이름")]
        public string label;

        [Tooltip("이 단계 이상이면 해제/노출")]
        public int unlockPhase = 1;

        [Header("처리 대상")]
        [Tooltip("잠금 전 숨겼다가 풀리면 노출할 UI")]
        public GameObject showHideTarget;

        [Tooltip("보이되 잠금 전 못 누르게 할 버튼")]
        public Selectable lockTarget;

        [Tooltip("잠금 시 스프라이트를 갈아끼울 아이콘")]
        public Image iconTarget;

        [Tooltip("잠겨있을 때 보여줄 잠금 스프라이트")]
        public Sprite lockedSprite;

        [NonSerialized] public Sprite normalSprite;
        [NonSerialized] public bool cached;
    }

    [Header("단계별 게이트 대상")]
    [SerializeField] private List<GatedElement> _elements = new();

    [Header("시작 단계(테스트용) / 시작 시 적용")]
    [SerializeField] private int _startPhase = 0;
    [SerializeField] private bool _applyOnStart = true;

    public int CurrentPhase { get; private set; }

    private void Awake()
    {
        // 아이콘 교체용 원본 스프라이트는 잠금 적용 전에 기억해둔다
        foreach (GatedElement el in _elements)
        {
            if (el?.iconTarget == null) continue;
            el.normalSprite = el.iconTarget.sprite;
            el.cached = true;
        }
    }

    private void Start()
    {
        if (_applyOnStart) SetPhase(_startPhase);
    }

    /// <summary>진행 단계를 적용한다(매니저가 호출).</summary>
    public void SetPhase(int phase)
    {
        CurrentPhase = phase;
        foreach (GatedElement el in _elements)
            Apply(el, phase);
    }

    private void Apply(GatedElement el, int phase)
    {
        if (el == null) return;
        bool unlocked = phase >= el.unlockPhase;

        if (el.showHideTarget != null)
            el.showHideTarget.SetActive(unlocked);

        if (el.lockTarget != null)
            el.lockTarget.interactable = unlocked;

        // 아이콘 스프라이트 교체는 원본을 잃지 않도록 플레이 중에만 (에디터 OnValidate 제외)
        if (el.iconTarget != null && el.cached && Application.isPlaying)
            el.iconTarget.sprite = unlocked || el.lockedSprite == null ? el.normalSprite : el.lockedSprite;
    }

#if UNITY_EDITOR
    // 인스펙터에서 _startPhase 바꾸면 에디터/플레이 중 바로 반영해 미리보기
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        foreach (GatedElement el in _elements)
            Apply(el, _startPhase);
    }

    // 컴포넌트 우클릭 메뉴로 단계 테스트
    [ContextMenu("단계 0")] private void _P0() => SetPhase(0);
    [ContextMenu("단계 1")] private void _P1() => SetPhase(1);
    [ContextMenu("단계 2")] private void _P2() => SetPhase(2);
    [ContextMenu("단계 3")] private void _P3() => SetPhase(3);
#endif
}
