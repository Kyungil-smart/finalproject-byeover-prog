//담당자: 조규민

using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 활성 가구 상호작용을 종료하는 화면 입력을 전달합니다.
/// </summary>
// 하우징 상호작용 종료 영역의 포인터 입력 전달
public class HousingInteractionExitView : MonoBehaviour, IPointerClickHandler
{
    public event Action OnClicked;

    // 활성 상호작용 종료 요청 이벤트 전달
    public void OnPointerClick(PointerEventData _eventData)
    {
        OnClicked?.Invoke();
    }

    // 상호작용 진행 여부에 따른 종료 입력 영역 표시 전환
    public void SetVisible(bool _isVisible)
    {
        gameObject.SetActive(_isVisible);
    }
}
