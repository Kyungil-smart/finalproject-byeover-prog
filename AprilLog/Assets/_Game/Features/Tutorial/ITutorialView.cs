// 담당자 : 정승우
// 설명   : 튜토리얼 View 인터페이스

using System;
using UnityEngine;

public interface ITutorialView
{
    void ShowStep(int stepIndex);
    void HighlightArea(Rect worldArea);
    void ClearHighlight();
    event Action OnStepCompleted;
}
