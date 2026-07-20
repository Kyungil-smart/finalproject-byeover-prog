//담당자: 조규민
// 수정 내용 : 미보유 가구 구매 입력을 슬롯 전체가 아닌 상태 버튼으로 분리하고 가격 상태에서만 버튼 레이캐스트를 켬

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 배치 아이템 한 칸을 표시합니다.
/// </summary>
// 배치 아이템 상태에 따른 아이콘·가격·잠금·선택·구매 버튼 표시 갱신
// 슬롯 클릭과 구매 클릭 이벤트를 구분해 Presenter에 전달
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
    [SerializeField] private Button _stateButton;

    private HousingPlacementItemData _itemData;
    private HousingPlacementItemState _itemState;

    public event Action<HousingPlacementItemData> OnClicked;
    public event Action<HousingPlacementItemData> OnPurchaseClicked;

    private void Awake()
    {
        ResolveReferences();
        Bind();
    }

    private void OnEnable()
    {
        SubscribeLocalization();
        RefreshLocalizedContent();
    }

    private void OnDisable()
    {
        UnsubscribeLocalization();
    }

    private void OnDestroy()
    {
        UnsubscribeLocalization();
        if (_slotButton != null)
        {
            _slotButton.onClick.RemoveListener(HandleClicked);
        }

        if (_stateButton != null)
        {
            _stateButton.onClick.RemoveListener(HandlePurchaseClicked);
        }
    }

    // ViewModel 성격의 아이템 데이터와 상태 기반 슬롯 전체 갱신
    public void SetData(HousingPlacementItemData _data, HousingPlacementItemState _state)
    {
        _itemData = _data;
        _itemState = _state;

        if (_data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        SetIcon(_data.Icon);
        SetName(_data);
        SetState(_data, _state);
        SetPurchaseButtonInteractable(_state == HousingPlacementItemState.Price);
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
            _nameText.text = ResolveLocalizedName(_data);
        }
    }

    // 잠금·구매 가능·보유·장착 상태별 UI 표시 전환
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
                SetStateVisual(_equippedStateSprite, _equippedStateColor, GetLocalizedText(13006));
                break;
            case HousingPlacementItemState.Owned:
                SetPreviewFrame(_defaultPreviewFrameSprite);
                SetStateVisual(_ownedStateSprite, _ownedStateColor, GetLocalizedText(13005));
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
        SetStateVisual(_priceStateSprite, _priceStateColor, GetLocalizedText(13007, FormatPrice(_data)));
        SetCurrencyIcon(ResolvePriceCurrencyIcon(_data));
        SetCurrencyVisible(true);
    }

    private void SubscribeLocalization()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLocalizedContent;
            LocalizationManager.Instance.OnLanguageChanged += RefreshLocalizedContent;
        }
    }

    private void UnsubscribeLocalization()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLocalizedContent;
        }
    }

    private void RefreshLocalizedContent()
    {
        if (_itemData == null)
        {
            return;
        }

        SetName(_itemData);
        SetState(_itemData, _itemState);
    }

    private static string ResolveLocalizedName(HousingPlacementItemData _data)
    {
        if (_data?.NameId > 0 && LocalizationManager.Instance != null)
        {
            string _localizedName = LocalizationManager.Instance.Get(_data.NameId, LocalizingType.Housing);

            if (!string.IsNullOrWhiteSpace(_localizedName) && !_localizedName.StartsWith("["))
            {
                return _localizedName;
            }
        }

        return string.IsNullOrWhiteSpace(_data?.DisplayName) ? _data?.ItemId ?? string.Empty : _data.DisplayName;
    }

    private static string GetLocalizedText(int _id, params object[] _args)
    {
        if (LocalizationManager.Instance == null)
        {
            return $"[{_id}]";
        }

        return _args == null || _args.Length == 0
            ? LocalizationManager.Instance.Get(_id, LocalizingType.UI)
            : LocalizationManager.Instance.Get(_id, LocalizingType.UI, _args);
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

        if (_stateButton == null)
        {
            _stateButton = transform.Find("StateButton_Image")?.GetComponent<Button>();
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

        if (_stateButton == null)
        {
            Debug.LogWarning("[HousingPlacementItemSlotView] 상태 Button 연결이 필요합니다.", this);
            return;
        }

        _stateButton.onClick.RemoveListener(HandlePurchaseClicked);
        _stateButton.onClick.AddListener(HandlePurchaseClicked);
    }

    private void HandleClicked()
    {
        if (_itemData == null)
        {
            return;
        }

        OnClicked?.Invoke(_itemData);
    }

    // 추가: 조규민 - 미보유 가구는 가격이 표시된 상태 버튼을 눌렀을 때만 구매 요청을 전달한다.
    private void HandlePurchaseClicked()
    {
        if (_itemData == null || _itemState != HousingPlacementItemState.Price)
        {
            return;
        }

        OnPurchaseClicked?.Invoke(_itemData);
    }

    private void SetPurchaseButtonInteractable(bool _isInteractable)
    {
        if (_stateButtonImage != null)
        {
            _stateButtonImage.raycastTarget = _isInteractable;
        }

        if (_stateButton != null)
        {
            _stateButton.interactable = _isInteractable;
        }
    }
}
