//담당자: 조규민

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 자동재화 가구 터치 입력을 Presenter로 전달합니다.
/// </summary>
public class HousingAutoCurrencyFurnitureView : MonoBehaviour, IPointerClickHandler
{
    [Header("입력")]
    [Tooltip("Image의 Raycast Target이 꺼져 있으면 클릭을 받을 수 없어 자동으로 켭니다.")]
    [SerializeField] private bool _ensureRaycastTarget = true;

    private Image _targetImage;

    public event Action OnClicked;

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
        EnsureClickable();
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        OnClicked?.Invoke();
    }

    private void EnsureClickable()
    {
        if (!_ensureRaycastTarget || _targetImage == null)
        {
            return;
        }

        _targetImage.raycastTarget = true;
    }
}
