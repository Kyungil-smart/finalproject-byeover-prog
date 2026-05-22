// 담당자 : 정승우
// 설명   : 튜토리얼 View -- 단계별 안내 표시

using System;
using UnityEngine;

public class TutorialView : MonoBehaviour, ITutorialView
{
    public event Action OnStepCompleted;

    [Header("UI")]
    [SerializeField] private GameObject _highlightMask;

    private TutorialPresenter _presenter;

    private void Awake()
    {
        _presenter = new TutorialPresenter(this);
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void ShowStep(int stepIndex) { /* 단계별 UI 표시 */ }
    public void HighlightArea(Rect worldArea) { /* 해당 영역만 밝게 */ }
    public void ClearHighlight() { if (_highlightMask != null) _highlightMask.SetActive(false); }

    // 유저가 터치하면 다음 단계로
    public void OnAreaTouched() => OnStepCompleted?.Invoke();
}
