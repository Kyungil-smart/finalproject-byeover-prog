//담당자: 조규민

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 광고 보상 가구의 클릭 입력을 전달합니다.
/// </summary>
public class HousingAdRewardButtonView : MonoBehaviour, IPointerClickHandler
{
    [Header("입력")]
    [Tooltip("Image의 Raycast Target이 꺼져 있으면 클릭을 받을 수 없어 자동으로 켭니다.")]
    [SerializeField] private bool _ensureRaycastTarget = true;

    private Image _targetImage;
    private bool _isInteractable = true;

    public event Action OnClicked;

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
        EnsureClickable();
    }

    private void OnValidate()
    {
        _targetImage = GetComponent<Image>();
        EnsureClickable();
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        if (!_isInteractable)
        {
            return;
        }

        OnClicked?.Invoke();
    }

    public void SetInteractable(bool _canInteract)
    {
        _isInteractable = _canInteract;

        if (_targetImage != null)
        {
            _targetImage.raycastTarget = _canInteract || _ensureRaycastTarget;
        }
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
