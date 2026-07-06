//담당자: 조규민

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 배치 버튼과 배치 모드 표시를 담당합니다.
/// </summary>
// 배치 모드 진입·종료 버튼 이벤트 전달과 모드 문구·팝업 표시 상태 갱신
public class HousingPlacementButtonView : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button _placementButton;
    [SerializeField] private Button _closeButton;

    [Header("상태 표시")]
    [SerializeField] private TextMeshProUGUI _placementModeText;
    [SerializeField] private TextMeshProUGUI _placementButtonLabelText;

    [Header("팝업")]
    [SerializeField] private GameObject _popupRoot;

    public event Action OnPlacementButtonClicked;
    public event Action OnCloseButtonClicked;

    private void Awake()
    {
        ResolveLocalizationReferences();
        Bind();
        SetPlacementMode(false);
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
        if (_placementButton != null)
        {
            _placementButton.onClick.RemoveListener(HandlePlacementButtonClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        }
    }

    private void ResolveLocalizationReferences()
    {
        if (_placementButtonLabelText == null && _placementButton != null)
        {
            _placementButtonLabelText = _placementButton.transform.Find("Text_Label")?.GetComponent<TextMeshProUGUI>();
        }
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
        if (_placementButtonLabelText != null && LocalizationManager.Instance != null)
        {
            _placementButtonLabelText.text = LocalizationManager.Instance.Get(13000, LocalizingType.UI);
        }
    }

    // 배치 모드 여부에 따른 팝업·버튼·안내 문구 표시 전환
    public void SetPlacementMode(bool _isActive)
    {
        if (_placementModeText != null)
        {
            _placementModeText.gameObject.SetActive(_isActive);
            _placementModeText.text = "가구 배치 중...";
        }

        SetPopupVisible(_isActive);
    }

    public void SetPopupVisible(bool _isVisible)
    {
        if (_popupRoot == null)
        {
            return;
        }

        _popupRoot.SetActive(_isVisible);
    }

    private void Bind()
    {
        if (_placementButton != null)
        {
            _placementButton.onClick.RemoveListener(HandlePlacementButtonClicked);
            _placementButton.onClick.AddListener(HandlePlacementButtonClicked);
        }

        if (_closeButton == null)
        {
            return;
        }

        _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        _closeButton.onClick.AddListener(HandleCloseButtonClicked);
    }

    // 배치 모드 전환 요청 이벤트 전달
    private void HandlePlacementButtonClicked()
    {
        OnPlacementButtonClicked?.Invoke();
    }

    private void HandleCloseButtonClicked()
    {
        OnCloseButtonClicked?.Invoke();
    }
}
