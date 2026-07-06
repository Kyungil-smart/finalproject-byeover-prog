//담당자: 조규민
// 하우징 시간 누적 보상 팝업의 표시와 입력 전달을 담당합니다.
// 시간당 생산량 표시는 누적 수령량이 아닌 Model의 생산량 값을 사용하도록 변경
// 중첩 프리팹에서 추가된 UI보다 팝업이 항상 위에 표시되도록 열 때 최종 형제 순서로 이동

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 누적 시간·보상 수량·게이지·수령 버튼 상태를 Model 데이터로 갱신
// 확인·취소 버튼과 다국어 변경 이벤트 등록·해제
public class HousingIdleRewardPopupView : MonoBehaviour
{
    [Header("루트")]
    [SerializeField] private GameObject _popupRoot;

    [Header("충전 게이지")]
    [SerializeField] private Image _gaugeFillImage;
    [SerializeField] private Slider _gaugeSlider;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private RectTransform _circleFillRect;

    [Header("상단 가구")]
    [SerializeField] private Image _furnitureIconImage;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _rewardTitleText;

    [Header("재화 아이콘")]
    [SerializeField] private Image _goldIconImage;
    [SerializeField] private Image _parchmentIconImage;
    [SerializeField] private Image _diamondIconImage;

    [Header("시간당 생산량")]
    [SerializeField] private TextMeshProUGUI _goldAmountText;
    [SerializeField] private TextMeshProUGUI _parchmentAmountText;
    [SerializeField] private TextMeshProUGUI _diamondAmountText;

    [Header("획득 보상 수량")]
    [SerializeField] private TextMeshProUGUI _goldRewardText;
    [SerializeField] private TextMeshProUGUI _parchmentRewardText;
    [SerializeField] private TextMeshProUGUI _diamondRewardText;

    [Header("버튼")]
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TextMeshProUGUI _confirmButtonText;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _cancelButtonText;

    public event Action OnConfirmClicked;
    public event Action OnCancelClicked;

    private HousingIdleRewardState _lastState;
    private bool _hasState;

    private void Awake()
    {
        ResolveRoot();
        ResolveLocalizationReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        SubscribeLocalization();
        UpdateLocalizedTexts();
    }

    private void OnDisable()
    {
        UnsubscribeLocalization();
    }

    private void OnDestroy()
    {
        UnsubscribeLocalization();
        UnbindButtons();
    }

    public void Show()
    {
        ResolveRoot();

        if (_popupRoot != null)
        {
            _popupRoot.transform.SetAsLastSibling();
            _popupRoot.SetActive(true);
        }
    }

    public void Hide()
    {
        ResolveRoot();

        if (_popupRoot != null)
        {
            _popupRoot.SetActive(false);
        }
    }

    // 방치 보상 상태 기반 게이지·시간·재화·확인 버튼 갱신
    public void Refresh(HousingIdleRewardState _state)
    {
        _lastState = _state;
        _hasState = true;
        SetGauge(_state.Progress);
        UpdateLocalizedTexts();
        SetConfirmInteractable(_state);
    }

    public void SetFurnitureIcon(Sprite _sprite)
    {
        if (_furnitureIconImage == null || _sprite == null)
        {
            return;
        }

        _furnitureIconImage.sprite = _sprite;
        _furnitureIconImage.enabled = true;
    }

    private void ResolveRoot()
    {
        if (_popupRoot == null)
        {
            _popupRoot = gameObject;
        }
    }

    private void ResolveLocalizationReferences()
    {
        Transform _root = _popupRoot != null ? _popupRoot.transform : transform;
        _rewardTitleText ??= FindChildText(_root, "Text_RewardTitle");
        _confirmButtonText ??= FindChildText(_confirmButton?.transform, "Text_Label");
        _cancelButtonText ??= FindChildText(_cancelButton?.transform, "Text_Label");
    }

    private void SubscribeLocalization()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedTexts;
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalizedTexts;
        }
    }

    private void UnsubscribeLocalization()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedTexts;
        }
    }

    private void UpdateLocalizedTexts()
    {
        if (LocalizationManager.Instance == null)
        {
            return;
        }

        SetText(_messageText, LocalizationManager.Instance.Get(13016, LocalizingType.UI));
        SetText(_rewardTitleText, LocalizationManager.Instance.Get(12040, LocalizingType.UI));
        SetText(_confirmButtonText, LocalizationManager.Instance.Get(11081, LocalizingType.UI));
        SetText(_cancelButtonText, LocalizationManager.Instance.Get(11082, LocalizingType.UI));

        if (!_hasState)
        {
            return;
        }

        SetProgressText(_lastState);
        SetRewardTexts(_lastState);
    }

    // 확인·취소 버튼 리스너 중복 제거 후 등록
    private void BindButtons()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            _confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (_cancelButton == null)
        {
            return;
        }

        _cancelButton.onClick.RemoveListener(HandleCancelClicked);
        _cancelButton.onClick.AddListener(HandleCancelClicked);
    }

    private void UnbindButtons()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        if (_cancelButton == null)
        {
            return;
        }

        _cancelButton.onClick.RemoveListener(HandleCancelClicked);
    }

    // 누적 진행률을 게이지 이미지와 슬라이더에 적용
    private void SetGauge(float _progress)
    {
        float _clampedProgress = Mathf.Clamp01(_progress);

        if (_gaugeFillImage != null)
        {
            _gaugeFillImage.fillAmount = _clampedProgress;
        }

        if (_circleFillRect != null)
        {
            float _fillEndX = Mathf.Lerp(0.06f, 0.94f, _clampedProgress);
            _circleFillRect.anchorMin = new Vector2(0.06f, 0.2f);
            _circleFillRect.anchorMax = new Vector2(_fillEndX, 0.8f);
            _circleFillRect.offsetMin = Vector2.zero;
            _circleFillRect.offsetMax = Vector2.zero;
        }

        if (_gaugeSlider == null)
        {
            return;
        }

        _gaugeSlider.minValue = 0f;
        _gaugeSlider.maxValue = 1f;
        _gaugeSlider.value = _clampedProgress;
    }

    private void SetProgressText(HousingIdleRewardState _state)
    {
        if (_progressText == null)
        {
            return;
        }

        _progressText.text = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.Get(13014, LocalizingType.UI, _state.ProgressPercent)
            : $"{_state.ProgressPercent}%";
    }

    private void SetRewardTexts(HousingIdleRewardState _state)
    {
        SetText(_goldAmountText, GetFormattedText(13018, FormatAmount(_state.GoldPerHour)));
        SetText(_parchmentAmountText, GetFormattedText(13018, FormatAmount(_state.ParchmentPerHour)));
        SetText(_diamondAmountText, GetFormattedText(13018, FormatAmount(_state.DiamondPerHour)));
        SetText(_goldRewardText, GetFormattedText(13018, FormatAmount(_state.GoldReward)));
        SetText(_parchmentRewardText, GetFormattedText(13018, FormatAmount(_state.ParchmentReward)));
        SetText(_diamondRewardText, GetFormattedText(13018, FormatAmount(_state.DiamondReward)));
    }

    private void SetConfirmInteractable(HousingIdleRewardState _state)
    {
        if (_confirmButton == null)
        {
            return;
        }

        _confirmButton.interactable = _state.HasClaimableReward;
    }

    private void SetText(TextMeshProUGUI _targetText, string _value)
    {
        if (_targetText == null)
        {
            return;
        }

        _targetText.text = _value;
    }

    private static string FormatAmount(int _amount)
    {
        _amount = Mathf.Max(0, _amount);

        if (_amount < 1000)
        {
            return _amount.ToString();
        }

        float _value = _amount / 1000f;
        return _value.ToString(_value % 1f == 0f ? "0" : "0.##") + "k";
    }

    private static string GetFormattedText(int _id, object _value)
    {
        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.Get(_id, LocalizingType.UI, _value)
            : _value?.ToString() ?? string.Empty;
    }

    private static TextMeshProUGUI FindChildText(Transform _root, string _name)
    {
        if (_root == null)
        {
            return null;
        }

        if (_root.name == _name)
        {
            return _root.GetComponent<TextMeshProUGUI>();
        }

        for (int _index = 0; _index < _root.childCount; _index++)
        {
            TextMeshProUGUI _found = FindChildText(_root.GetChild(_index), _name);

            if (_found != null)
            {
                return _found;
            }
        }

        return null;
    }

    private void HandleConfirmClicked()
    {
        OnConfirmClicked?.Invoke();
    }

    private void HandleCancelClicked()
    {
        OnCancelClicked?.Invoke();
    }
}
