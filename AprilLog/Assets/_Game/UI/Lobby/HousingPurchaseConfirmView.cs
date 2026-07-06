//담당자: 조규민

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 가구 구매 확인과 재화 부족 안내를 표시합니다.
/// </summary>
// 구매 확인·취소 입력 전달과 재화 부족 안내 팝업 표시
// 버튼·다국어 이벤트 등록 해제 및 지연 숨김 코루틴 정리
public class HousingPurchaseConfirmView : MonoBehaviour
{
    [Header("구매 확인 팝업")]
    [SerializeField] private GameObject _confirmPopupRoot;
    [SerializeField] private TextMeshProUGUI _confirmMessageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TextMeshProUGUI _confirmButtonText;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _cancelButtonText;

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
        ResolveLocalizationReferences();
        BindButtons();
        HideAll();
    }

    private void OnEnable()
    {
        SubscribeLocalization();
        UpdateLocalizedTexts();
    }

    private void OnDisable()
    {
        UnsubscribeLocalization();
        HideAll();
    }

    private void OnDestroy()
    {
        UnsubscribeLocalization();
        UnbindButtons();
    }

    private void ResolveLocalizationReferences()
    {
        _confirmButtonText ??= _confirmButton?.transform.Find("Text_Label")?.GetComponent<TextMeshProUGUI>();
        _cancelButtonText ??= _cancelButton?.transform.Find("Text_Label")?.GetComponent<TextMeshProUGUI>();
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

        if (_confirmButtonText != null)
        {
            _confirmButtonText.text = LocalizationManager.Instance.Get(11081, LocalizingType.UI);
        }

        if (_cancelButtonText != null)
        {
            _cancelButtonText.text = LocalizationManager.Instance.Get(11082, LocalizingType.UI);
        }
    }

    // 선택 아이템 구매 안내 문구 설정과 확인 패널 표시
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

    // 재화 부족 안내 표시와 자동 숨김 코루틴 시작
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

    // 확인·취소 버튼 이벤트 중복 제거 후 등록
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
