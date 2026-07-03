//담당자: 조규민

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InGameConfirmPopupButtonType
{
    None,
    Yes,
    No
}

/// <summary>
/// 인게임 확인 팝업 표시와 입력 이벤트 전달을 담당한다.
/// </summary>
public class InGameConfirmPopupView : MonoBehaviour
{
    [Serializable]
    private class ConfirmButtonVisual
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Sprite _defaultBackgroundSprite;
        [SerializeField] private Sprite _selectedBackgroundSprite;
        [SerializeField] private Color _defaultTextColor = Color.white;
        [SerializeField] private Color _selectedTextColor = Color.black;

        public void Resolve(Button button)
        {
            if (button == null)
            {
                return;
            }

            if (_backgroundImage == null)
            {
                _backgroundImage = ResolveBackgroundImage(button);
            }

            if (_text == null)
            {
                _text = button.GetComponentInChildren<TMP_Text>(true);
            }
        }

        public void Apply(bool isSelected)
        {
            ApplySprite(_backgroundImage, isSelected ? _selectedBackgroundSprite : _defaultBackgroundSprite);
            ApplyTextColor(_text, isSelected ? _selectedTextColor : _defaultTextColor);
        }

        private static Image ResolveBackgroundImage(Button button)
        {
            if (button.targetGraphic is Image targetImage)
            {
                return targetImage;
            }

            return button.GetComponent<Image>();
        }

        private static void ApplySprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
        }

        private static void ApplyTextColor(TMP_Text text, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
        }
    }

    [Header("팝업")]
    [SerializeField] private GameObject _rootObject;
    [SerializeField] private TMP_Text _messageText;

    [Header("버튼")]
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;
    [SerializeField] private Button _closeButton;

    [Header("선택 이미지")]
    [SerializeField] private ConfirmButtonVisual _yesVisual = new ConfirmButtonVisual();
    [SerializeField] private ConfirmButtonVisual _noVisual = new ConfirmButtonVisual();

    public event Action OnYesClicked;
    public event Action OnNoClicked;
    public event Action OnCloseClicked;

    private void Awake()
    {
        ResolveReferences();
        ResolveVisualReferences();
        ShowSelectedButton(InGameConfirmPopupButtonType.None);
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

        if (isVisible)
        {
            ShowSelectedButton(InGameConfirmPopupButtonType.None);
        }
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

    private void ResolveVisualReferences()
    {
        _yesVisual.Resolve(_yesButton);
        _noVisual.Resolve(_noButton);
    }

    public void ShowSelectedButton(InGameConfirmPopupButtonType selectedButtonType)
    {
        _yesVisual.Apply(selectedButtonType == InGameConfirmPopupButtonType.Yes);
        _noVisual.Apply(selectedButtonType == InGameConfirmPopupButtonType.No);
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
        ShowSelectedButton(InGameConfirmPopupButtonType.Yes);
        OnYesClicked?.Invoke();
    }

    private void HandleNoClicked()
    {
        ShowSelectedButton(InGameConfirmPopupButtonType.No);
        OnNoClicked?.Invoke();
    }

    private void HandleCloseClicked()
    {
        ShowSelectedButton(InGameConfirmPopupButtonType.None);
        OnCloseClicked?.Invoke();
    }
}
