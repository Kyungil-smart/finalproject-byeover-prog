//담당자: 조규민

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 가구 구매 확인과 재화 부족 안내를 표시합니다.
/// </summary>
public class HousingPurchaseConfirmView : MonoBehaviour
{
    [Header("구매 확인 팝업")]
    [SerializeField] private GameObject _confirmPopupRoot;
    [SerializeField] private TextMeshProUGUI _confirmMessageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Header("재화 부족 안내")]
    [SerializeField] private GameObject _insufficientNoticeRoot;
    [SerializeField] private TextMeshProUGUI _insufficientMessageText;
    [SerializeField] private string _insufficientCurrencyMessage = "현재 재화가 부족합니다.";
    [Tooltip("재화 부족 안내가 화면에 유지되는 시간입니다.")]
    [SerializeField] private float _insufficientNoticeDuration = 1.5f;

    private Coroutine _insufficientNoticeCoroutine;

    public event Action OnConfirmClicked;
    public event Action OnCancelClicked;

    private void Awake()
    {
        BindButtons();
        HideAll();
    }

    private void OnDisable()
    {
        HideAll();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void ShowConfirmation(string _message)
    {
        StopInsufficientNotice();

        if (_confirmMessageText != null)
        {
            _confirmMessageText.text = _message;
        }

        SetButtonsInteractable(true);
        SetActive(_confirmPopupRoot, true);
    }

    public void HideConfirmation()
    {
        SetActive(_confirmPopupRoot, false);
        SetButtonsInteractable(true);
    }

    public void SetButtonsInteractable(bool _isInteractable)
    {
        if (_confirmButton != null)
        {
            _confirmButton.interactable = _isInteractable;
        }

        if (_cancelButton != null)
        {
            _cancelButton.interactable = _isInteractable;
        }
    }

    public void ShowInsufficientCurrency()
    {
        StopInsufficientNotice();

        if (_insufficientMessageText != null)
        {
            _insufficientMessageText.text = _insufficientCurrencyMessage;
        }

        SetActive(_insufficientNoticeRoot, true);
        _insufficientNoticeCoroutine = StartCoroutine(HideInsufficientNoticeAfterDelay());
    }

    public void HideAll()
    {
        StopInsufficientNotice();
        SetActive(_confirmPopupRoot, false);
        SetActive(_insufficientNoticeRoot, false);
        SetButtonsInteractable(true);
    }

    private void BindButtons()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            _confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(HandleCancelClicked);
            _cancelButton.onClick.AddListener(HandleCancelClicked);
        }
    }

    private void UnbindButtons()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(HandleCancelClicked);
        }
    }

    private IEnumerator HideInsufficientNoticeAfterDelay()
    {
        float _duration = Mathf.Max(0f, _insufficientNoticeDuration);

        if (_duration > 0f)
        {
            yield return new WaitForSecondsRealtime(_duration);
        }

        SetActive(_insufficientNoticeRoot, false);
        _insufficientNoticeCoroutine = null;
    }

    private void StopInsufficientNotice()
    {
        if (_insufficientNoticeCoroutine == null)
        {
            return;
        }

        StopCoroutine(_insufficientNoticeCoroutine);
        _insufficientNoticeCoroutine = null;
    }

    private void HandleConfirmClicked()
    {
        OnConfirmClicked?.Invoke();
    }

    private void HandleCancelClicked()
    {
        OnCancelClicked?.Invoke();
    }

    private static void SetActive(GameObject _target, bool _isActive)
    {
        if (_target != null && _target.activeSelf != _isActive)
        {
            _target.SetActive(_isActive);
        }
    }
}
