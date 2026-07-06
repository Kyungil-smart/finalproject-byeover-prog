//담당자: 조규민
// 하우징 시간 누적 보상 팝업의 표시와 입력 전달을 담당합니다.
// 시간당 생산량 표시는 누적 수령량이 아닌 Model의 생산량 값을 사용하도록 변경
// 중첩 프리팹에서 추가된 UI보다 팝업이 항상 위에 표시되도록 열 때 최종 형제 순서로 이동

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Button _cancelButton;

    public event Action OnConfirmClicked;
    public event Action OnCancelClicked;

    private void Awake()
    {
        ResolveRoot();
        BindButtons();
    }

    private void OnDestroy()
    {
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

    public void Refresh(HousingIdleRewardState _state)
    {
        SetGauge(_state.Progress);
        SetProgressText(_state);
        SetRewardTexts(_state);
        SetConfirmInteractable(_state);

        if (_messageText != null)
        {
            _messageText.text = "누적된 재화를 수령하시겠습니까?";
        }
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

        _progressText.text = $"{_state.ProgressPercent}%";
    }

    private void SetRewardTexts(HousingIdleRewardState _state)
    {
        SetText(_goldAmountText, FormatAmount(_state.GoldPerHour) + "/h");
        SetText(_parchmentAmountText, FormatAmount(_state.ParchmentPerHour) + "/h");
        SetText(_diamondAmountText, FormatAmount(_state.DiamondPerHour) + "/h");
        SetText(_goldRewardText, FormatAmount(_state.GoldReward));
        SetText(_parchmentRewardText, FormatAmount(_state.ParchmentReward));
        SetText(_diamondRewardText, FormatAmount(_state.DiamondReward));
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

    private void HandleConfirmClicked()
    {
        OnConfirmClicked?.Invoke();
    }

    private void HandleCancelClicked()
    {
        OnCancelClicked?.Invoke();
    }
}
