//담당자: 조규민
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 벽지 색상 슬롯의 표시 상태와 클릭 이벤트 전달을 담당한다.
/// </summary>
public class HousingWallpaperSlotView : MonoBehaviour
{
    [Header("슬롯 UI")]
    [SerializeField] private Button _button;
    [SerializeField] private Image _colorImage;
    [SerializeField] private Image _statusBackground;

    [Header("상태 색상")]
    [SerializeField] private Color _normalStatusColor = Color.white;
    [SerializeField] private Color _selectedStatusColor = new Color(0f, 0.85f, 0.32f, 1f);

    public event Action<int> Clicked;

    private int _slotIndex;

    private void Awake()
    {
        ResolveReferences();
        ValidateReferences();

        if (_button != null)
            _button.onClick.AddListener(NotifyClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(NotifyClicked);
    }

    public void SetData(int _index, Color _color, bool _isSelected)
    {
        _slotIndex = _index;

        if (_colorImage != null)
            _colorImage.color = _color;

        if (_statusBackground != null)
            _statusBackground.color = _isSelected ? _selectedStatusColor : _normalStatusColor;
    }

    private void NotifyClicked()
    {
        Clicked?.Invoke(_slotIndex);
    }

    private void ResolveReferences()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_colorImage == null)
        {
            Transform _color = transform.Find("Image_Color");
            if (_color != null)
                _colorImage = _color.GetComponent<Image>();
        }

        if (_statusBackground == null)
        {
            Transform _status = transform.Find("Image_Status");
            if (_status != null)
                _statusBackground = _status.GetComponent<Image>();
        }
    }

    private void ValidateReferences()
    {
        if (_button == null)
            Debug.LogWarning("[HousingWallpaperSlotView] 슬롯 Button이 연결되지 않았습니다.", this);

        if (_colorImage == null)
            Debug.LogWarning("[HousingWallpaperSlotView] 색상 Image가 연결되지 않았습니다.", this);

        if (_statusBackground == null)
            Debug.LogWarning("[HousingWallpaperSlotView] 상태 배경 Image가 연결되지 않았습니다.", this);
    }
}
