//담당자: 조규민
// 광고 상태 기반 보상 팝업 문구·아이콘·버튼 입력 가능 여부 갱신
// 버튼과 다국어 변경 이벤트의 수명 주기별 등록·해제

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 광고 보기 팝업의 표시와 입력 전달을 담당합니다.
/// </summary>
public class HousingAdRewardPopupView : MonoBehaviour
{
    [Header("루트")]
    [SerializeField] private GameObject _popupRoot;

    [Header("상단 가구")]
    [SerializeField] private Image _rewardFurnitureIconImage;
    [SerializeField] private TextMeshProUGUI _rewardFurnitureTitleText;
    [SerializeField] private TextMeshProUGUI _messageText;

    [Header("보상 영역")]
    [SerializeField] private TextMeshProUGUI _rewardTitleText;
    [SerializeField] private GameObject _rewardArea;
    [SerializeField] private TextMeshProUGUI _heartRewardAmountText;
    [SerializeField] private TextMeshProUGUI _diamondRewardAmountText;

    [Header("버튼")]
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TextMeshProUGUI _confirmButtonText;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _cancelButtonText;

    public event Action OnConfirmClicked;
    public event Action OnCancelClicked;

    private int _heartRewardAmount;
    private int _diamondRewardAmount;

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

    // 광고 상태 데이터 기반 안내 문구와 확인 버튼 상태 갱신
    public void Refresh(HousingAdRewardState _state)
    {
        UpdateLocalizedTexts();

        if (_confirmButton != null)
        {
            _confirmButton.interactable = _state.CanConfirm;
        }

        SetVisible(_state.IsVisible);
    }

    public void SetRewardAmounts(int _heartAmount, int _diamondAmount)
    {
        _heartRewardAmount = Mathf.Max(0, _heartAmount);
        _diamondRewardAmount = Mathf.Max(0, _diamondAmount);
        UpdateLocalizedTexts();
    }

    public void SetFurnitureIcon(Sprite _sprite)
    {
        if (_rewardFurnitureIconImage == null || _sprite == null)
        {
            return;
        }

        _rewardFurnitureIconImage.sprite = _sprite;
        _rewardFurnitureIconImage.enabled = true;
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
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
        _rewardFurnitureTitleText ??= FindChildText(_rewardFurnitureIconImage?.transform, "Text (TMP)");
        _heartRewardAmountText ??= FindChildText(FindChild(_root, "RewardSlot_Heart"), "Text_RewardAmount");
        _diamondRewardAmountText ??= FindChildText(FindChild(_root, "RewardSlot_Diamond"), "Text_RewardAmount");
    }

    // 언어 변경 시 팝업 고정 문구 갱신을 위한 이벤트 등록
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

        SetText(_rewardFurnitureTitleText, LocalizationManager.Instance.Get(13020, LocalizingType.UI));
        SetText(_messageText, LocalizationManager.Instance.Get(13021, LocalizingType.UI));
        SetText(_rewardTitleText, LocalizationManager.Instance.Get(12040, LocalizingType.UI));
        SetText(_heartRewardAmountText, LocalizationManager.Instance.Get(13022, LocalizingType.UI, _heartRewardAmount));
        SetText(_diamondRewardAmountText, LocalizationManager.Instance.Get(13023, LocalizingType.UI, _diamondRewardAmount));
        SetText(_confirmButtonText, LocalizationManager.Instance.Get(11081, LocalizingType.UI));
        SetText(_cancelButtonText, LocalizationManager.Instance.Get(11082, LocalizingType.UI));
    }

    // 확인·취소 버튼 클릭 이벤트 중복 방지 후 등록
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

    private void SetVisible(bool _isVisible)
    {
        ResolveRoot();

        if (_popupRoot == null)
        {
            return;
        }

        _popupRoot.SetActive(_isVisible);
    }

    private static void SetText(TextMeshProUGUI _targetText, string _value)
    {
        if (_targetText == null)
        {
            return;
        }

        _targetText.text = _value;
    }

    private static Transform FindChild(Transform _root, string _name)
    {
        if (_root == null)
        {
            return null;
        }

        if (_root.name == _name)
        {
            return _root;
        }

        for (int _index = 0; _index < _root.childCount; _index++)
        {
            Transform _found = FindChild(_root.GetChild(_index), _name);

            if (_found != null)
            {
                return _found;
            }
        }

        return null;
    }

    private static TextMeshProUGUI FindChildText(Transform _root, string _name)
    {
        return FindChild(_root, _name)?.GetComponent<TextMeshProUGUI>();
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
