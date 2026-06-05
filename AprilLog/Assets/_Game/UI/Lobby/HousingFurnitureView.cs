//담당자: 조규민
//설명: 하우징 페이지에 배치된 임시 가구의 터치/클릭 이벤트와 상호작용 표시 데이터를 전달한다.

using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 하우징 임시 가구의 선택 이벤트와 표시 데이터를 담당한다.
/// </summary>
public class HousingFurnitureView : MonoBehaviour, IPointerClickHandler
{
    [Header("가구 정보")]
    [SerializeField] private string _furnitureName = "임시 가구";

    [Header("상호작용 설정")]
    [Tooltip("플레이어가 가구와 상호작용하기 위해 이동할 위치")]
    [SerializeField] private RectTransform _interactionPoint;
    [Tooltip("가구 상호작용 시 Housing_InteractionTexture 위치에 표시할 문구")]
    [SerializeField] private string _interactionMessage = "가구와 상호작용했습니다.";

    public event Action<HousingFurnitureView> Clicked;

    public string FurnitureName => _furnitureName;
    public string InteractionMessage => _interactionMessage;

    public Vector2 GetInteractionPosition()
    {
        RectTransform _rectTransform = _interactionPoint != null
            ? _interactionPoint
            : transform as RectTransform;

        if (_rectTransform == null)
        {
            Debug.LogWarning("[HousingFurnitureView] 상호작용 위치를 찾지 못했습니다.", this);
            return Vector2.zero;
        }

        return _rectTransform.anchoredPosition;
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        Clicked?.Invoke(this);
    }
}
