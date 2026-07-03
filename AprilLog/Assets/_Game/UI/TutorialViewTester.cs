// 매니저 없이 TutorialView를 직접 구동해 오버레이를 검증하는 테스트용
using UnityEngine;

public class TutorialViewTester : MonoBehaviour
{
    [SerializeField] private TutorialView _view;
    [SerializeField] private TutorialStepData _stepData;
    [SerializeField] private int _index;

    [ContextMenu("이 단계 표시")]
    private void Show()
    {
        if (_view == null || _stepData == null) return;
        _index = Mathf.Clamp(_index, 0, _stepData.Count - 1);
        _view.ShowStep(_stepData.Get(_index));
        Debug.Log($"[튜토 테스트] {_index}단계 표시");
    }

    [ContextMenu("다음 단계")]
    private void Next() { _index++; Show(); }

    [ContextMenu("이전 단계")]
    private void Prev() { _index--; Show(); }

    [ContextMenu("숨기기")]
    private void HideView() => _view?.Hide();
}
