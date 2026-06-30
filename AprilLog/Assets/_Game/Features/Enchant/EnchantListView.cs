// 생성자 : 김영찬
// 인첸트 목록 UI를 구동하기 위한 스크립트 -- View

// 2차 수정자 : 조규민
// 수정 내용 :
// 선택된 인챈트 목록 탭 버튼의 배경과 텍스트 색상을 상태에 맞게 갱신
// 보유 인챈트 선택 시 상세 정보 테이블이 갱신되도록 선택 이벤트와 빈 슬롯 방어 추가
// 보유 인챈트 설명 영역인 EnchantChangeGuideText도 선택된 인챈트 설명으로 갱신

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
    [Tooltip("스킬 인챈트 선택 시 이름, 타입, 설명, 아이콘을 표시할 정보 테이블")]
    [SerializeField] private EnchantChangeInfoTableUI _skillInfoTable;
    [Tooltip("스킬 인챈트 선택 시 설명을 표시할 안내 텍스트")]
    [SerializeField] private TMP_Text _skillGuideText;
    
    [Header("Stat Panel")]
    [Tooltip("인첸트 변경때 사용한 버튼과 구성이 동일")]
    [SerializeField] Button[] _statChangeButtons;
    [Tooltip("스탯 인챈트 선택 시 이름, 타입, 설명, 아이콘을 표시할 정보 테이블")]
    [SerializeField] private EnchantChangeInfoTableUI _statInfoTable;
    [Tooltip("스탯 인챈트 선택 시 설명을 표시할 안내 텍스트")]
    [SerializeField] private TMP_Text _statGuideText;

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
    private const string _emptyGuideText = "인챈트를 선택하세요";
    private bool _isSkillTabSelected = true;
    
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;
    
    public event Action<bool> OnEnabled;
    public event Action<EnchantDisplayData> OnSkillEnchantSelected;
    public event Action<EnchantDisplayData> OnStatEnchantSelected;

    public void Init()
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
    
    public void SetOwnedSkillList(List<EnchantDisplayData> _ownedSkillList)
    {
        if(_skillChangeButtons == null)
        {
            Debug.LogWarning("SkillSelectButtons Not Serialized");
            return;
        }

        _skillInfoTable = ResolveInfoTable(_skillInfoTable, _skillPanel, _skillChangeButtons);
        _skillGuideText = ResolveGuideText(_skillGuideText, _skillPanel);
        SetOwnedEnchantList(_skillChangeButtons, _ownedSkillList, HandleSkillEnchantSelected);
    }

    public void SetOwnedStatList(List<EnchantDisplayData> _ownedStatList)
    {
        if(_statChangeButtons == null)
        {
            Debug.LogWarning("StatSelectButtons Not Serialized");
            return;
        }

        _statInfoTable = ResolveInfoTable(_statInfoTable, _statPanel, _statChangeButtons);
        _statGuideText = ResolveGuideText(_statGuideText, _statPanel);
        SetOwnedEnchantList(_statChangeButtons, _ownedStatList, HandleStatEnchantSelected);
    }

    public void SetSelectedSkillEnchantInfo(EnchantDisplayData _selectedData)
    {
        if (_selectedData == null)
        {
            ClearSelectedSkillEnchantInfo();
            return;
        }

        _skillInfoTable = ResolveInfoTable(_skillInfoTable, _skillPanel, _skillChangeButtons);
        _skillGuideText = ResolveGuideText(_skillGuideText, _skillPanel);
        SetSelectedEnchantInfo(_skillInfoTable, _selectedData, "SkillInfoTable Not Serialized");
        SetGuideText(_skillGuideText, _selectedData.Description);
    }

    public void SetSelectedStatEnchantInfo(EnchantDisplayData _selectedData)
    {
        if (_selectedData == null)
        {
            ClearSelectedStatEnchantInfo();
            return;
        }

        _statInfoTable = ResolveInfoTable(_statInfoTable, _statPanel, _statChangeButtons);
        _statGuideText = ResolveGuideText(_statGuideText, _statPanel);
        SetSelectedEnchantInfo(_statInfoTable, _selectedData, "StatInfoTable Not Serialized");
        SetGuideText(_statGuideText, _selectedData.Description);
    }

    public void ClearSelectedSkillEnchantInfo()
    {
        _skillInfoTable = ResolveInfoTable(_skillInfoTable, _skillPanel, _skillChangeButtons);
        _skillGuideText = ResolveGuideText(_skillGuideText, _skillPanel);
        _skillInfoTable?.ClearInfo();
        SetGuideText(_skillGuideText, _emptyGuideText);
    }

    public void ClearSelectedStatEnchantInfo()
    {
        _statInfoTable = ResolveInfoTable(_statInfoTable, _statPanel, _statChangeButtons);
        _statGuideText = ResolveGuideText(_statGuideText, _statPanel);
        _statInfoTable?.ClearInfo();
        SetGuideText(_statGuideText, _emptyGuideText);
    }

    private void SetOwnedEnchantList(Button[] _buttons, List<EnchantDisplayData> _ownedList, Action<EnchantDisplayData> _onSelected)
    {
        int _ownedCount = _ownedList == null ? 0 : _ownedList.Count;

        for (int _index = 0; _index < _buttons.Length; _index++)
        {
            Button _button = _buttons[_index];
            if (_button == null)
            {
                continue;
            }

            EnchantChangeSelectButtonUI _buttonUI = _button.GetComponent<EnchantChangeSelectButtonUI>();
            if (_buttonUI == null)
            {
                _button.interactable = false;
                continue;
            }

            _buttonUI.OnEnchantDisplaySelected -= _onSelected;

            bool _hasData = _index < _ownedCount && _ownedList[_index] != null;
            _button.interactable = _hasData;
            if (!_hasData)
            {
                _buttonUI.ClearInfo();
                continue;
            }

            _buttonUI.SetInfo(_ownedList[_index]);
            _buttonUI.OnEnchantDisplaySelected += _onSelected;
        }
    }

    private EnchantChangeInfoTableUI ResolveInfoTable(EnchantChangeInfoTableUI _currentTable, GameObject _panel, Button[] _buttons)
    {
        if (_currentTable != null)
        {
            return _currentTable;
        }

        if (_panel != null)
        {
            EnchantChangeInfoTableUI _panelTable = _panel.GetComponentInChildren<EnchantChangeInfoTableUI>(true);
            if (_panelTable != null)
            {
                return _panelTable;
            }
        }

        return GetInfoTableFromButtons(_buttons);
    }

    private EnchantChangeInfoTableUI GetInfoTableFromButtons(Button[] _buttons)
    {
        if (_buttons == null)
        {
            return null;
        }

        for (int _index = 0; _index < _buttons.Length; _index++)
        {
            Button _button = _buttons[_index];
            if (_button == null)
            {
                continue;
            }

            EnchantChangeSelectButtonUI _buttonUI = _button.GetComponent<EnchantChangeSelectButtonUI>();
            if (_buttonUI != null && _buttonUI.InfoTableUI != null)
            {
                return _buttonUI.InfoTableUI;
            }
        }

        return null;
    }

    private TMP_Text ResolveGuideText(TMP_Text _currentText, GameObject _panel)
    {
        if (_currentText != null)
        {
            return _currentText;
        }

        if (_panel == null)
        {
            return null;
        }

        TMP_Text[] _texts = _panel.GetComponentsInChildren<TMP_Text>(true);
        for (int _index = 0; _index < _texts.Length; _index++)
        {
            TMP_Text _text = _texts[_index];
            if (_text != null && _text.gameObject.name == "EnchantChangeGuideText (TMP)")
            {
                return _text;
            }
        }

        return null;
    }

    private void SetGuideText(TMP_Text _guideText, string _text)
    {
        if (_guideText == null)
        {
            return;
        }

        _guideText.text = string.IsNullOrWhiteSpace(_text) ? _emptyGuideText : _text;
    }

    private void SetSelectedEnchantInfo(EnchantChangeInfoTableUI _infoTable, EnchantDisplayData _selectedData, string _warningMessage)
    {
        if (_infoTable == null)
        {
            Debug.LogWarning(_warningMessage);
            return;
        }

        _infoTable.SetInfo(_selectedData);
    }

    private void HandleSkillEnchantSelected(EnchantDisplayData _selectedData)
    {
        OnSkillEnchantSelected?.Invoke(_selectedData);
    }

    private void HandleStatEnchantSelected(EnchantDisplayData _selectedData)
    {
        OnStatEnchantSelected?.Invoke(_selectedData);
    }
}
