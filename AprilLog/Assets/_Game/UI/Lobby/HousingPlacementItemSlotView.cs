//담당자: 조규민

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 배치 아이템 한 칸을 표시합니다.
/// </summary>
public class HousingPlacementItemSlotView : MonoBehaviour
{
    [Header("표시 요소")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _stateText;

    [Header("입력")]
    [SerializeField] private Button _slotButton;

    private HousingPlacementItemData _itemData;

    public event Action<HousingPlacementItemData> OnClicked;

    private void Awake()
    {
        ResolveReferences();
        Bind();
    }

    private void OnDestroy()
    {
        if (_slotButton == null)
        {
            return;
        }

        _slotButton.onClick.RemoveListener(HandleClicked);
    }

    public void SetData(HousingPlacementItemData _data)
    {
        _itemData = _data;

        if (_data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        SetIcon(_data.Icon);
        SetText(_data);
    }

    private void SetIcon(Sprite _icon)
    {
        if (_iconImage == null)
        {
            return;
        }

        _iconImage.sprite = _icon;
        _iconImage.enabled = true;
        _iconImage.color = _icon != null ? Color.white : new Color(0.85f, 0.87f, 0.89f, 1f);
    }

    private void SetText(HousingPlacementItemData _data)
    {
        if (_nameText != null)
        {
            _nameText.text = string.IsNullOrWhiteSpace(_data.DisplayName) ? _data.ItemId : _data.DisplayName;
        }

        if (_stateText == null)
        {
            return;
        }

        if (_data.IsOwned)
        {
            _stateText.text = "보유중";
            return;
        }

        _stateText.text = _data.IsUnlocked ? $"{_data.Price:N0}" : "잠김";
    }

    private void ResolveReferences()
    {
        if (_slotButton == null)
        {
            _slotButton = GetComponent<Button>();
        }

        if (_iconImage == null)
        {
            _iconImage = transform.Find("PreviewIcon_Image")?.GetComponent<Image>();
        }

        if (_nameText == null)
        {
            _nameText = transform.Find("ItemName_Text")?.GetComponent<TextMeshProUGUI>();
        }

        if (_stateText == null)
        {
            _stateText = transform.Find("OwnershipOrPrice_Text")?.GetComponent<TextMeshProUGUI>();
        }
    }

    private void Bind()
    {
        if (_slotButton == null)
        {
            Debug.LogWarning("[HousingPlacementItemSlotView] 슬롯 Button 연결이 필요합니다.", this);
            return;
        }

        _slotButton.onClick.RemoveListener(HandleClicked);
        _slotButton.onClick.AddListener(HandleClicked);
    }

    private void HandleClicked()
    {
        if (_itemData == null)
        {
            return;
        }

        OnClicked?.Invoke(_itemData);
    }
}
