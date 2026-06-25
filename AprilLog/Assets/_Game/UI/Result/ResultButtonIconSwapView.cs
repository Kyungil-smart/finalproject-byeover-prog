//담당자: 조규민
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

    private void OnEnable()
    {
        SetNormalIcon();
    }

    private void OnDisable()
    {
        SetNormalIcon();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_iconImage == null || _pressedIcon == null)
        {
            return;
        }

        _iconImage.sprite = _pressedIcon;
    }

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
