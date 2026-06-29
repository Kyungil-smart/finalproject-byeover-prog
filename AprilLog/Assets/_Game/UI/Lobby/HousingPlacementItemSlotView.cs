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
    [SerializeField] private Image _previewFrameImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _stateButtonImage;
    [SerializeField] private Image _currencyIconImage;
    [SerializeField] private TextMeshProUGUI _stateText;

    // 추가: 조규민 - 장착, 보유, 미구매 상태에 맞춰 프레임과 상태 버튼 애셋을 함께 변경한다.
    [Header("미리보기 프레임")]
    [SerializeField] private Sprite _equippedPreviewFrameSprite;
    [SerializeField] private Sprite _defaultPreviewFrameSprite;

    [Header("상태 배경")]
    [SerializeField] private Sprite _equippedStateSprite;
    [SerializeField] private Sprite _ownedStateSprite;
    [SerializeField] private Sprite _priceStateSprite;
    [SerializeField] private Sprite _lockedStateSprite;

    [Header("구매 아이콘")]
    [SerializeField] private Sprite _goldPriceIconSprite;
    [SerializeField] private Sprite _diamondPriceIconSprite;

    [Header("상태 색상")]
    [SerializeField] private Color _equippedStateColor = new(0.20f, 0.78f, 0.38f, 1f);
    [SerializeField] private Color _ownedStateColor = new(0.34f, 0.85f, 0.48f, 1f);
    [SerializeField] private Color _priceStateColor = new(0.92f, 0.25f, 0.43f, 1f);
    [SerializeField] private Color _lockedStateColor = new(0.42f, 0.42f, 0.42f, 1f);
    [SerializeField] private Color _stateTextColor = Color.white;

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

    public void SetData(HousingPlacementItemData _data, HousingPlacementItemState _state)
    {
        _itemData = _data;

        if (_data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        SetIcon(_data.Icon);
        SetName(_data);
        SetState(_data, _state);
    }

    private void SetIcon(Sprite _icon)
    {
        if (_iconImage == null)
        {
            return;
        }

        _iconImage.sprite = _icon;
        _iconImage.preserveAspect = true;
        _iconImage.enabled = _icon != null;
        _iconImage.color = _icon != null ? Color.white : new Color(0.85f, 0.87f, 0.89f, 1f);
    }

    private void SetName(HousingPlacementItemData _data)
    {
        if (_nameText != null)
        {
            _nameText.text = string.IsNullOrWhiteSpace(_data.DisplayName) ? _data.ItemId : _data.DisplayName;
        }
    }

    private void SetState(HousingPlacementItemData _data, HousingPlacementItemState _state)
    {
        if (_stateText == null)
        {
            return;
        }

        _stateText.color = _stateTextColor;
        SetCurrencyVisible(false);

        switch (_state)
        {
            case HousingPlacementItemState.Equipped:
                SetPreviewFrame(_equippedPreviewFrameSprite);
                SetStateVisual(_equippedStateSprite, _equippedStateColor, "장착됨");
                break;
            case HousingPlacementItemState.Owned:
                SetPreviewFrame(_defaultPreviewFrameSprite);
                SetStateVisual(_ownedStateSprite, _ownedStateColor, "보유중");
                break;
            case HousingPlacementItemState.Price:
                SetPreviewFrame(_defaultPreviewFrameSprite);
                SetPriceState(_data);
                break;
            default:
                SetPreviewFrame(_defaultPreviewFrameSprite);
                SetStateVisual(_lockedStateSprite, _lockedStateColor, "잠금");
                break;
        }
    }

    private void SetPreviewFrame(Sprite _sprite)
    {
        if (_previewFrameImage == null)
        {
            return;
        }

        _previewFrameImage.sprite = _sprite;
        _previewFrameImage.color = Color.white;
        _previewFrameImage.enabled = _sprite != null;
    }

    private void SetStateVisual(Sprite _sprite, Color _color, string _text)
    {
        if (_stateButtonImage != null)
        {
            _stateButtonImage.sprite = _sprite;
            _stateButtonImage.color = _sprite != null ? Color.white : _color;
            _stateButtonImage.type = _sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            _stateButtonImage.enabled = true;
        }

        _stateText.text = _text;
    }

    private void SetPriceState(HousingPlacementItemData _data)
    {
        SetStateVisual(_priceStateSprite, _priceStateColor, FormatPrice(_data));
        SetCurrencyIcon(ResolvePriceCurrencyIcon(_data));
        SetCurrencyVisible(true);
    }

    private string FormatPrice(HousingPlacementItemData _data)
    {
        if (_data == null)
        {
            return "0";
        }

        return Mathf.Max(0, _data.Price).ToString("N0");
    }

    private Sprite ResolvePriceCurrencyIcon(HousingPlacementItemData _data)
    {
        if (_data != null && _data.PriceCurrency == HousingPlacementPriceCurrency.Diamond)
        {
            return _diamondPriceIconSprite != null ? _diamondPriceIconSprite : _goldPriceIconSprite;
        }

        return _goldPriceIconSprite;
    }

    private void SetCurrencyIcon(Sprite _sprite)
    {
        if (_currencyIconImage == null)
        {
            return;
        }

        _currencyIconImage.sprite = _sprite;
        _currencyIconImage.color = Color.white;
        _currencyIconImage.preserveAspect = true;
        _currencyIconImage.enabled = _sprite != null;
    }

    private void SetCurrencyVisible(bool _isVisible)
    {
        if (_currencyIconImage != null)
        {
            _currencyIconImage.gameObject.SetActive(_isVisible);
        }
    }

    private void ResolveReferences()
    {
        if (_slotButton == null)
        {
            _slotButton = GetComponent<Button>();
        }

        if (_iconImage == null)
        {
            _iconImage = transform.Find("PreviewFrame_Image/PreviewIcon_Image")?.GetComponent<Image>();
        }

        if (_previewFrameImage == null)
        {
            _previewFrameImage = transform.Find("PreviewFrame_Image")?.GetComponent<Image>();
        }

        if (_nameText == null)
        {
            _nameText = transform.Find("ItemName_Text")?.GetComponent<TextMeshProUGUI>();
        }

        if (_stateText == null)
        {
            _stateText = transform.Find("StateButton_Image/OwnershipOrPrice_Text")?.GetComponent<TextMeshProUGUI>();
        }

        if (_stateButtonImage == null)
        {
            _stateButtonImage = transform.Find("StateButton_Image")?.GetComponent<Image>();
        }

        if (_currencyIconImage == null)
        {
            _currencyIconImage = transform.Find("StateButton_Image/CurrencyIcon_Image")?.GetComponent<Image>();
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
