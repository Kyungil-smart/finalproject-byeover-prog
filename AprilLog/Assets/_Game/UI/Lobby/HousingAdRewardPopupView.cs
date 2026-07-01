//담당자: 조규민

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

    [Header("Inspector 값 유지")]
    [Tooltip("켜져 있으면 Play 시 하위 TextMeshPro 텍스트를 코드 값으로 덮어쓰지 않습니다.")]
    [SerializeField] private bool _keepInspectorTextValues = true;

    [Header("상단 가구")]
    [SerializeField] private Image _rewardFurnitureIconImage;
    [SerializeField] private TextMeshProUGUI _messageText;

    [Header("보상 영역")]
    [SerializeField] private TextMeshProUGUI _rewardTitleText;
    [SerializeField] private GameObject _rewardArea;

    [Header("버튼")]
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TextMeshProUGUI _confirmButtonText;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _cancelButtonText;

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

    public void Refresh(HousingAdRewardState _state)
    {
        if (!_keepInspectorTextValues)
        {
            SetText(_rewardTitleText, _state.RewardTitle);
            SetText(_confirmButtonText, _state.ConfirmText);
            SetText(_cancelButtonText, _state.CancelText);
        }

        string _displayMessage = string.IsNullOrWhiteSpace(_state.StatusMessage)
            ? _state.Message
            : _state.StatusMessage;
        SetText(_messageText, _displayMessage);

        if (_confirmButton != null)
        {
            _confirmButton.interactable = _state.CanConfirm;
        }

        SetVisible(_state.IsVisible);
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

    private void HandleConfirmClicked()
    {
        OnConfirmClicked?.Invoke();
    }

    private void HandleCancelClicked()
    {
        OnCancelClicked?.Invoke();
    }
}
