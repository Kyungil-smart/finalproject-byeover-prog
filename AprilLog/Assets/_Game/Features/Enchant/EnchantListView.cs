// 생성자 : 김영찬
// 인첸트 목록 UI를 구동하기 위한 스크립트 -- View

// 2차 수정자 : 조규민
// 수정 내용 : 선택된 인챈트 목록 탭 버튼의 배경과 텍스트 색상을 상태에 맞게 갱신

using System;
using System.Collections.Generic;
using Firebase.AppCheck;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EnchantListView : MonoBehaviour, IEnchantListView
{
    [Header("UI Elements")]
    [SerializeField] Button _skillSelectButton;
    [SerializeField] Button _statSelectButton;
    [SerializeField] GameObject _skillPanel;
    [SerializeField] GameObject _statPanel;
    [Tooltip("옵션 버튼은 Pause 버튼을 눌러서 진입 되었을때만 활성화")]
    [SerializeField] GameObject _linkButtonBoundary;

    [Header("선택 버튼 스타일")]
    [Tooltip("선택된 버튼 배경입니다. 비워두면 Skill 선택 버튼의 시작 스프라이트를 사용합니다.")]
    [SerializeField] private Sprite _selectedButtonSprite;
    [Tooltip("선택되지 않은 버튼 배경입니다. 비워두면 Stat 선택 버튼의 시작 스프라이트를 사용합니다.")]
    [SerializeField] private Sprite _unselectedButtonSprite;
    [SerializeField] private Color _selectedTextColor = Color.black;
    [SerializeField] private Color _unselectedTextColor = Color.white;

    [Header("Skill Panel")]
    [Tooltip("인첸트 변경때 사용한 버튼과 구성이 동일")]
    [SerializeField] Button[] _skillChangeButtons;
    
    [Header("Stat Panel")]
    [Tooltip("인첸트 변경때 사용한 버튼과 구성이 동일")]
    [SerializeField] Button[] _statChangeButtons;

    [Header("참조")] 
    [Tooltip("옵션 버튼의 활성화를 위함 : 스킬 선택창에서 넘어오면 이 UI가 활성화 > 정지 버튼을 눌러서 진입한것이 아니게 됨")]
    [SerializeField] GameObject _skillSelectUI;
    [SerializeField] EnchantUIModel _model;
    
    private EnchantListPresenter _presenter;
    public EnchantListPresenter Presenter => _presenter;

    private Image _skillSelectButtonImage;
    private Image _statSelectButtonImage;
    private TMP_Text _skillSelectButtonText;
    private TMP_Text _statSelectButtonText;
    private bool _isSkillTabSelected = true;
    
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;
    
    public event Action<bool> OnEnabled;

    private void Awake()
    {
        CacheSelectButtonStyleReferences();

        if (!_isInitialized)
        {
            _presenter = new EnchantListPresenter(_model, this);
            _isInitialized = true;
        }
    }

    private void OnEnable()
    {
        ToggleOptionButtonSet();
        OnSkillSelectButtonClick();
        OnEnabled?.Invoke(true);
    }

    private void OnDisable()
    {
        OnEnabled?.Invoke(false);
    }

    private void LateUpdate()
    {
        ApplySelectButtonStyle(_isSkillTabSelected);
    }
    
    private void ToggleOptionButtonSet()
    {
        if(_linkButtonBoundary == null) return;

        if (_skillSelectUI != null && _skillSelectUI.activeInHierarchy)
        {
            _linkButtonBoundary.SetActive(false);
        }
        else
        {
            _linkButtonBoundary.SetActive(true);
        }
    }
    
    public void OnSkillSelectButtonClick()
    {
        _isSkillTabSelected = true;
        _skillSelectButton.interactable = true;
        _statSelectButton.interactable = true;
        _skillPanel.SetActive(true);
        _statPanel.SetActive(false);
        ApplySelectButtonStyle(true);
    }

    public void OnStatSelectButtonClick()
    {
        _isSkillTabSelected = false;
        _skillSelectButton.interactable = true;
        _statSelectButton.interactable = true;
        _skillPanel.SetActive(false);
        _statPanel.SetActive(true);
        ApplySelectButtonStyle(false);
    }

    private void CacheSelectButtonStyleReferences()
    {
        _skillSelectButtonImage = GetButtonImage(_skillSelectButton);
        _statSelectButtonImage = GetButtonImage(_statSelectButton);
        _skillSelectButtonText = GetButtonText(_skillSelectButton);
        _statSelectButtonText = GetButtonText(_statSelectButton);
        NormalizeSelectButtonTint(_skillSelectButton);
        NormalizeSelectButtonTint(_statSelectButton);
        _selectedTextColor = Color.black;
        _unselectedTextColor = Color.white;

        if (_selectedButtonSprite == null && _skillSelectButtonImage != null)
        {
            _selectedButtonSprite = _skillSelectButtonImage.sprite;
        }

        if (_unselectedButtonSprite == null && _statSelectButtonImage != null)
        {
            _unselectedButtonSprite = _statSelectButtonImage.sprite;
        }
    }

    private Image GetButtonImage(Button button)
    {
        if (button == null)
        {
            return null;
        }

        return button.GetComponent<Image>();
    }

    private TMP_Text GetButtonText(Button button)
    {
        if (button == null)
        {
            return null;
        }

        return button.GetComponentInChildren<TMP_Text>(true);
    }

    private void NormalizeSelectButtonTint(Button button)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        button.colors = colors;
    }

    private void ApplySelectButtonStyle(bool isSkillSelected)
    {
        SetSelectButtonStyle(_skillSelectButtonImage, _skillSelectButtonText, isSkillSelected);
        SetSelectButtonStyle(_statSelectButtonImage, _statSelectButtonText, !isSkillSelected);
    }

    private void SetSelectButtonStyle(Image buttonImage, TMP_Text buttonText, bool isSelected)
    {
        if (buttonImage != null)
        {
            Sprite targetSprite = isSelected ? _selectedButtonSprite : _unselectedButtonSprite;
            buttonImage.sprite = targetSprite;
            buttonImage.overrideSprite = targetSprite;
            buttonImage.color = Color.white;
        }

        if (buttonText != null)
        {
            buttonText.color = isSelected ? _selectedTextColor : _unselectedTextColor;
        }
    }
    
    public void SetOwnedSkillList(List<EnchantDisplayData> ownedSkillList)
    {
        if(_skillChangeButtons == null || _statChangeButtons == null)
        {
            Debug.LogWarning("SkillSelectButtons Not Serialized");
            return;
        }

        if (ownedSkillList.Count > 0)
        {
            for (int i = 0; i < _skillChangeButtons.Length; i++)
            {
                _skillChangeButtons[i].interactable = ownedSkillList.Count > i;
                if(_skillChangeButtons[i].gameObject.activeInHierarchy)
                {
                    var buttonUI = _skillChangeButtons[i].GetComponent<EnchantChangeSelectButtonUI>();
                    buttonUI.SetInfo(ownedSkillList[i]);
                }
            }
        }
    }

    public void SetOwnedStatList(List<EnchantDisplayData> ownedStatList)
    {
        if (ownedStatList.Count > 0)
        {
            for (int i = 0; i < _statChangeButtons.Length; i++)
            {
                _skillChangeButtons[i].interactable = ownedStatList.Count > i;
                if(_skillChangeButtons[i].gameObject.activeInHierarchy)
                {
                    var buttonUI = _skillChangeButtons[i].GetComponent<EnchantChangeSelectButtonUI>();
                    buttonUI.SetInfo(ownedStatList[i]);
                }
            }
        }
    }
}
