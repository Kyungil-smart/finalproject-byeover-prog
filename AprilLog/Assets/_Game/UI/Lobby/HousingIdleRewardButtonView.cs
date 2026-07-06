//담당자: 조규민

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 시간 누적 보상 가구의 클릭 입력을 Presenter로 전달합니다.
/// </summary>
// 방치 보상 버튼 클릭 입력 전달을 위한 포인터 이벤트 연결
public class HousingIdleRewardButtonView : MonoBehaviour, IPointerClickHandler
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

    // 방치 보상 팝업 열기 요청 이벤트 전달
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
