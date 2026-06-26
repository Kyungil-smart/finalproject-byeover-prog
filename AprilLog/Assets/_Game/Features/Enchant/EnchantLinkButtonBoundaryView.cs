//담당자: 조규민

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EnchantLinkButtonType
{
    None,
    Continue,
    ReturnLobby,
    RestartChapter
}

/// <summary>
/// 인챈트 링크 버튼 입력 이벤트를 전달한다.
/// </summary>
public class EnchantLinkButtonBoundaryView : MonoBehaviour
{
    [Serializable]
    private class LinkButtonVisual
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Sprite _defaultBackgroundSprite;
        [SerializeField] private Sprite _selectedBackgroundSprite;
        [SerializeField] private Sprite _defaultIconSprite;
        [SerializeField] private Sprite _selectedIconSprite;

        public void Resolve(GameObject buttonSetObject, Button fallbackButton)
        {
            if (_backgroundImage == null)
            {
                _backgroundImage = FindBackgroundImage(buttonSetObject, fallbackButton);
            }

            if (_iconImage == null)
            {
                _iconImage = FindIconImage(buttonSetObject, _backgroundImage);
            }
        }

        public void Apply(bool isSelected)
        {
            ApplySprite(_backgroundImage, isSelected ? _selectedBackgroundSprite : _defaultBackgroundSprite);
            ApplySprite(_iconImage, isSelected ? _selectedIconSprite : _defaultIconSprite);
        }

        private static Image FindBackgroundImage(GameObject buttonSetObject, Button fallbackButton)
        {
            if (fallbackButton != null && fallbackButton.targetGraphic is Image targetImage)
            {
                return targetImage;
            }

            if (fallbackButton != null)
            {
                Image buttonImage = fallbackButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    return buttonImage;
                }
            }

            if (buttonSetObject == null)
            {
                return null;
            }

            return buttonSetObject.GetComponentInChildren<Image>(true);
        }

        private static Image FindIconImage(GameObject buttonSetObject, Image backgroundImage)
        {
            if (buttonSetObject == null)
            {
                return null;
            }

            Image[] images = buttonSetObject.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image == null || image == backgroundImage)
                {
                    continue;
                }

                if (image.name.Contains("Image (1)"))
                {
                    return image;
                }
            }

            foreach (Image image in images)
            {
                if (image != null && image != backgroundImage)
                {
                    return image;
                }
            }

            return null;
        }

        private static void ApplySprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
        }
    }

    [Header("버튼")]
    [SerializeField] private GameObject _continueButtonSet;
    [SerializeField] private Button _continueButton;
    [SerializeField] private GameObject _returnLobbyButtonSet;
    [SerializeField] private Button _returnLobbyButton;
    [SerializeField] private GameObject _restartChapterButtonSet;
    [SerializeField] private Button _restartChapterButton;

    [Header("선택 이미지")]
    [SerializeField] private LinkButtonVisual _continueVisual = new LinkButtonVisual();
    [SerializeField] private LinkButtonVisual _returnLobbyVisual = new LinkButtonVisual();
    [SerializeField] private LinkButtonVisual _restartChapterVisual = new LinkButtonVisual();

    [Header("참조")]
    [SerializeField] private ScreenNavigator _screenNavigator;
    [SerializeField] private InGameConfirmPopupView _confirmPopupView;

    private InGameConfirmPopupPresenter _confirmPopupPresenter;
    private EnchantLinkButtonBoundaryPresenter _presenter;
    private readonly List<Button> _boundContinueButtons = new List<Button>();
    private readonly List<Button> _boundReturnLobbyButtons = new List<Button>();
    private readonly List<Button> _boundRestartChapterButtons = new List<Button>();

    public event Action OnContinueClicked;
    public event Action OnReturnLobbyClicked;
    public event Action OnRestartChapterClicked;

    private void Awake()
    {
        ResolveReferences();
        ResolveVisualReferences();
        ShowSelectedButton(EnchantLinkButtonType.None);
        BindButtons();
        CreatePresenters();
    }

    private void OnDestroy()
    {
        UnbindButtons();
        _presenter?.Dispose();
        _confirmPopupPresenter?.Dispose();
    }

    private void ResolveReferences()
    {
        if (_continueButtonSet == null)
        {
            _continueButtonSet = FindDirectChildObject("ContinueButtonSet");
        }

        if (_returnLobbyButtonSet == null)
        {
            _returnLobbyButtonSet = FindDirectChildObject("ReturnLobbyButtonSet");
        }

        if (_restartChapterButtonSet == null)
        {
            _restartChapterButtonSet = FindDirectChildObject("RestartChapterButtonSet");
        }

        if (_screenNavigator == null)
        {
            _screenNavigator = FindFirstObjectByType<ScreenNavigator>();
        }

        if (_confirmPopupView == null)
        {
            _confirmPopupView = FindFirstObjectByType<InGameConfirmPopupView>(FindObjectsInactive.Include);
        }
    }

    private GameObject FindDirectChildObject(string objectName)
    {
        Transform child = transform.Find(objectName);
        if (child == null)
        {
            return null;
        }

        return child.gameObject;
    }

    private void ResolveVisualReferences()
    {
        _continueVisual.Resolve(_continueButtonSet, _continueButton);
        _returnLobbyVisual.Resolve(_returnLobbyButtonSet, _returnLobbyButton);
        _restartChapterVisual.Resolve(_restartChapterButtonSet, _restartChapterButton);
    }

    public void ShowSelectedButton(EnchantLinkButtonType selectedButtonType)
    {
        _continueVisual.Apply(selectedButtonType == EnchantLinkButtonType.Continue);
        _returnLobbyVisual.Apply(selectedButtonType == EnchantLinkButtonType.ReturnLobby);
        _restartChapterVisual.Apply(selectedButtonType == EnchantLinkButtonType.RestartChapter);
    }

    private void CreatePresenters()
    {
        if (_confirmPopupView != null)
        {
            InGameConfirmPopupModel _confirmPopupModel = new InGameConfirmPopupModel();
            _confirmPopupPresenter = new InGameConfirmPopupPresenter(_confirmPopupModel, _confirmPopupView);
        }

        _presenter = new EnchantLinkButtonBoundaryPresenter(this, _screenNavigator, _confirmPopupPresenter);
    }

    private void BindButtons()
    {
        BindButtonGroup(_continueButtonSet, _continueButton, _boundContinueButtons, HandleContinueClicked);
        BindButtonGroup(_returnLobbyButtonSet, _returnLobbyButton, _boundReturnLobbyButtons, HandleReturnLobbyClicked);
        BindButtonGroup(_restartChapterButtonSet, _restartChapterButton, _boundRestartChapterButtons, HandleRestartChapterClicked);
    }

    private void UnbindButtons()
    {
        UnbindButtonGroup(_boundContinueButtons, HandleContinueClicked);
        UnbindButtonGroup(_boundReturnLobbyButtons, HandleReturnLobbyClicked);
        UnbindButtonGroup(_boundRestartChapterButtons, HandleRestartChapterClicked);
    }

    private void BindButtonGroup(GameObject buttonSetObject, Button fallbackButton, List<Button> boundButtons, UnityEngine.Events.UnityAction action)
    {
        boundButtons.Clear();

        if (buttonSetObject != null)
        {
            Button[] buttons = buttonSetObject.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                BindButton(button, boundButtons, action);
            }
        }

        BindButton(fallbackButton, boundButtons, action);
    }

    private void BindButton(Button button, List<Button> boundButtons, UnityEngine.Events.UnityAction action)
    {
        if (button == null || boundButtons.Contains(button))
        {
            return;
        }

        button.onClick.AddListener(action);
        boundButtons.Add(button);
    }

    private void UnbindButtonGroup(List<Button> boundButtons, UnityEngine.Events.UnityAction action)
    {
        foreach (Button button in boundButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        boundButtons.Clear();
    }

    private void HandleContinueClicked()
    {
        OnContinueClicked?.Invoke();
    }

    private void HandleReturnLobbyClicked()
    {
        OnReturnLobbyClicked?.Invoke();
    }

    private void HandleRestartChapterClicked()
    {
        OnRestartChapterClicked?.Invoke();
    }
}
