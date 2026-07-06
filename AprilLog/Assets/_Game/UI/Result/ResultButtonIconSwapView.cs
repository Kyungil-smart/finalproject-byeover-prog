//담당자: 조규민
// 포인터 누름·해제·이탈 상태에 따른 결과 화면 버튼 아이콘 교체
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 정산 버튼 아이콘의 기본/누름 상태 이미지를 전환한다.
/// </summary>
public class ResultButtonIconSwapView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("아이콘")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Sprite _normalIcon;
    [SerializeField] private Sprite _pressedIcon;

    // 활성화 시 기본 아이콘 복원
    private void OnEnable()
    {
        SetNormalIcon();
    }

    private void OnDisable()
    {
        SetNormalIcon();
    }

    // 버튼 누름 상태 아이콘 적용
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_iconImage == null || _pressedIcon == null)
        {
            return;
        }

        _iconImage.sprite = _pressedIcon;
    }

    // 포인터 해제 시 기본 아이콘 복원
    public void OnPointerUp(PointerEventData eventData)
    {
        SetNormalIcon();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetNormalIcon();
    }

    private void SetNormalIcon()
    {
        if (_iconImage == null || _normalIcon == null)
        {
            return;
        }

        _iconImage.sprite = _normalIcon;
    }
}
