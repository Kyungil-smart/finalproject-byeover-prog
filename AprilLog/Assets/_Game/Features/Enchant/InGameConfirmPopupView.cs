//담당자: 조규민

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 확인 팝업 표시와 입력 이벤트 전달을 담당한다.
/// </summary>
public class InGameConfirmPopupView : MonoBehaviour
{
    [Header("팝업")]
    [SerializeField] private GameObject _rootObject;
    [SerializeField] private TMP_Text _messageText;

    [Header("버튼")]
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;
    [SerializeField] private Button _closeButton;

    public event Action OnYesClicked;
    public event Action OnNoClicked;
    public event Action OnCloseClicked;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void SetMessage(string message)
    {
        if (_messageText == null)
        {
            return;
        }

        _messageText.text = message;
    }

    public void SetVisible(bool isVisible)
    {
        GameObject _targetObject = _rootObject != null ? _rootObject : gameObject;
        _targetObject.SetActive(isVisible);
    }

    private void ResolveReferences()
    {
        if (_rootObject == null)
        {
            _rootObject = gameObject;
        }

        if (_messageText == null)
        {
            _messageText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void BindButtons()
    {
        if (_yesButton != null)
        {
            _yesButton.onClick.AddListener(HandleYesClicked);
        }

        if (_noButton != null)
        {
            _noButton.onClick.AddListener(HandleNoClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(HandleCloseClicked);
        }
    }

    private void UnbindButtons()
    {
        if (_yesButton != null)
        {
            _yesButton.onClick.RemoveListener(HandleYesClicked);
        }

        if (_noButton != null)
        {
            _noButton.onClick.RemoveListener(HandleNoClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleCloseClicked);
        }
    }

    private void HandleYesClicked()
    {
        OnYesClicked?.Invoke();
    }

    private void HandleNoClicked()
    {
        OnNoClicked?.Invoke();
    }

    private void HandleCloseClicked()
    {
        OnCloseClicked?.Invoke();
    }
}
