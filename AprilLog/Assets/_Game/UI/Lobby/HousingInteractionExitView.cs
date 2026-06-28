//담당자: 조규민

using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 활성 가구 상호작용을 종료하는 화면 입력을 전달합니다.
/// </summary>
public class HousingInteractionExitView : MonoBehaviour, IPointerClickHandler
{
    public event Action OnClicked;

    public void OnPointerClick(PointerEventData _eventData)
    {
        OnClicked?.Invoke();
    }

    public void SetVisible(bool _isVisible)
    {
        gameObject.SetActive(_isVisible);
    }
}
