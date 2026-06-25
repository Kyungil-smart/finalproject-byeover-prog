//담당자: 조규민

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 배치 버튼과 배치 모드 표시를 담당합니다.
/// </summary>
public class HousingPlacementButtonView : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button _placementButton;
    [SerializeField] private Button _closeButton;

    [Header("상태 표시")]
    [SerializeField] private TextMeshProUGUI _placementModeText;

    [Header("팝업")]
    [SerializeField] private GameObject _popupRoot;

    public event Action OnPlacementButtonClicked;
    public event Action OnCloseButtonClicked;

    private void Awake()
    {
        Bind();
        SetPlacementMode(false);
    }

    private void OnDestroy()
    {
        if (_placementButton != null)
        {
            _placementButton.onClick.RemoveListener(HandlePlacementButtonClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        }
    }

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

    private void HandlePlacementButtonClicked()
    {
        OnPlacementButtonClicked?.Invoke();
    }

    private void HandleCloseButtonClicked()
    {
        OnCloseButtonClicked?.Invoke();
    }
}
