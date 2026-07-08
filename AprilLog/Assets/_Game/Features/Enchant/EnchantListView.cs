// 생성자 : 김영찬
// 인첸트 목록 UI를 구동하기 위한 스크립트 -- View

// 2차 수정자 : 조규민
// 수정 내용 :
// 선택된 인챈트 목록 탭 버튼의 배경과 텍스트 색상을 상태에 맞게 갱신
// 보유 인챈트 선택 시 상세 정보 테이블이 갱신되도록 선택 이벤트와 빈 슬롯 방어 추가
// 보유 인챈트 설명 영역인 EnchantChangeGuideText도 선택된 인챈트 설명으로 갱신

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 수정 내용 : 로비 복귀 후 이어하기에서 인챈트 획득 플로우 없이 보유 인챈트 팝업을 열어도 Presenter가 초기화되도록 보강
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
    
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;
    
    public event Action<bool> OnEnabled;
    public event Action<EnchantDisplayData> OnSkillEnchantSelected;
    public event Action<EnchantDisplayData> OnStatEnchantSelected;

    private void Awake()
    {
        HideEmptyEnchantListState();
    }

    public void Init()
    {
        CacheSelectButtonStyleReferences();
        ResolvePanelReferences();

        if (!_isInitialized)
        {
            if (_model == null)
            {
                _model = FindFirstObjectByType<EnchantUIModel>(FindObjectsInactive.Include);
            }

            if (_model == null)
            {
                Debug.LogError("[EnchantListView] EnchantUIModel 참조가 없어 보유 인챈트 목록을 갱신할 수 없습니다.", this);
                return;
            }

            _presenter = new EnchantListPresenter(_model, this);
            _isInitialized = true;
        }
    }

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            Init();
        }

        ToggleOptionButtonSet();
        HideEmptyEnchantListState();
        OnSkillSelectButtonClick();
        OnEnabled?.Invoke(true);
    }

    private void OnDisable()
    {
        OnEnabled?.Invoke(false);
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
        ResolvePanelReferences();
        SetButtonInteractable(_skillSelectButton, true);
        SetButtonInteractable(_statSelectButton, true);
        SetPanelActive(_skillPanel, true);
        SetPanelActive(_statPanel, false);
        ApplySelectButtonStyle(true);
    }

    public void OnStatSelectButtonClick()
    {
        ResolvePanelReferences();
        SetButtonInteractable(_skillSelectButton, true);
        SetButtonInteractable(_statSelectButton, true);
        SetPanelActive(_skillPanel, false);
        SetPanelActive(_statPanel, true);
        ApplySelectButtonStyle(false);
    }

    private void SetButtonInteractable(Button _button, bool _interactable)
    {
        if (_button == null)
        {
            return;
        }

        _button.interactable = _interactable;
    }

    private void SetPanelActive(GameObject _panel, bool _active)
    {
        if (_panel == null)
        {
            return;
        }

        _panel.SetActive(_active);
    }

    private void ResolvePanelReferences()
    {
        _skillInfoTable = ResolveInfoTable(_skillInfoTable, _skillPanel, _skillChangeButtons);
        _statInfoTable = ResolveInfoTable(_statInfoTable, _statPanel, _statChangeButtons);
        _skillGuideText = ResolveGuideText(_skillGuideText, _skillPanel);
        _statGuideText = ResolveGuideText(_statGuideText, _statPanel);
    }

    private void HideEmptyEnchantListState()
    {
        ResolvePanelReferences();
        ClearOwnedEnchantButtons(_skillChangeButtons);
        ClearOwnedEnchantButtons(_statChangeButtons);
        ClearSelectedSkillEnchantInfo();
        ClearSelectedStatEnchantInfo();
    }

    private void ClearOwnedEnchantButtons(Button[] _buttons)
    {
        if (_buttons == null)
        {
            return;
        }

        for (int _index = 0; _index < _buttons.Length; _index++)
        {
            Button _button = _buttons[_index];
            if (_button == null)
            {
                continue;
            }

            _button.interactable = false;
            EnchantChangeSelectButtonUI _buttonUI = _button.GetComponent<EnchantChangeSelectButtonUI>();
            if (_buttonUI != null)
            {
                _buttonUI.ClearInfo();
            }
        }
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
        SetInfoAreaIconAndLevel(_skillPanel, "SkillEnchantInfoImage", _selectedData);
        SetInfoAreaTagText(_skillPanel, _selectedData);
        SetGuideText(_skillGuideText, BuildGuideText(_selectedData.Description));
        SetSelectedButton(_skillChangeButtons, _selectedData);
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
        SetInfoAreaIconAndLevel(_statPanel, "StatEnchantInfoImage", _selectedData);
        SetInfoAreaTagText(_statPanel, _selectedData);
        SetGuideText(_statGuideText, BuildGuideText(_selectedData.Description));
        SetSelectedButton(_statChangeButtons, _selectedData);
    }

    public void ClearSelectedSkillEnchantInfo()
    {
        _skillInfoTable = ResolveInfoTable(_skillInfoTable, _skillPanel, _skillChangeButtons);
        _skillGuideText = ResolveGuideText(_skillGuideText, _skillPanel);
        _skillInfoTable?.ClearInfo();
        ClearInfoAreaIconAndLevel(_skillPanel, "SkillEnchantInfoImage");
        ClearInfoAreaTagText(_skillPanel);
        ClearGuideText(_skillGuideText);
        SetSelectedButton(_skillChangeButtons, null);
    }

    public void ClearSelectedStatEnchantInfo()
    {
        _statInfoTable = ResolveInfoTable(_statInfoTable, _statPanel, _statChangeButtons);
        _statGuideText = ResolveGuideText(_statGuideText, _statPanel);
        _statInfoTable?.ClearInfo();
        ClearInfoAreaIconAndLevel(_statPanel, "StatEnchantInfoImage");
        ClearInfoAreaTagText(_statPanel);
        ClearGuideText(_statGuideText);
        SetSelectedButton(_statChangeButtons, null);
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

    private void SetSelectedButton(Button[] _buttons, EnchantDisplayData _selectedData)
    {
        if (_buttons == null)
        {
            return;
        }

        EnchantChangeSelectButtonUI _selectedButton = FindSelectedButton(_buttons, _selectedData);
        for (int _index = 0; _index < _buttons.Length; _index++)
        {
            Button _button = _buttons[_index];
            EnchantChangeSelectButtonUI _buttonUI = _button == null
                ? null
                : _button.GetComponent<EnchantChangeSelectButtonUI>();

            if (_buttonUI == null)
            {
                continue;
            }

            _buttonUI.SetSelected(_buttonUI == _selectedButton);
        }
    }

    private EnchantChangeSelectButtonUI FindSelectedButton(Button[] _buttons, EnchantDisplayData _selectedData)
    {
        if (_selectedData == null)
        {
            return null;
        }

        EnchantChangeSelectButtonUI _matchingButton = null;
        for (int _index = 0; _index < _buttons.Length; _index++)
        {
            Button _button = _buttons[_index];
            EnchantChangeSelectButtonUI _buttonUI = _button == null
                ? null
                : _button.GetComponent<EnchantChangeSelectButtonUI>();

            EnchantDisplayData _buttonData = _buttonUI == null
                ? null
                : _buttonUI.EnchantDisplayData;
            if (ReferenceEquals(_buttonData, _selectedData))
            {
                return _buttonUI;
            }

            if (_matchingButton == null
                && _buttonData != null
                && _buttonData.EnchantId == _selectedData.EnchantId
                && _buttonData.Level == _selectedData.Level)
            {
                _matchingButton = _buttonUI;
            }
        }

        return _matchingButton;
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

        if (!_guideText.gameObject.activeSelf)
        {
            _guideText.gameObject.SetActive(true);
        }

        _guideText.enabled = true;
        _guideText.text = string.IsNullOrWhiteSpace(_text) ? _emptyGuideText : _text;
    }

    private void ClearGuideText(TMP_Text _guideText)
    {
        if (_guideText == null)
        {
            return;
        }

        _guideText.text = string.Empty;
        _guideText.enabled = false;
        _guideText.gameObject.SetActive(false);
    }

    private void SetInfoAreaIconAndLevel(GameObject _panel, string _imageObjectName, EnchantDisplayData _selectedData)
    {
        Image _infoImage = FindImageInPanel(_panel, _imageObjectName);
        if (_infoImage == null)
        {
            Debug.LogWarning($"[EnchantListView] InfoArea 상세 이미지를 찾을 수 없습니다: {_imageObjectName}", this);
            return;
        }

        if (!_infoImage.gameObject.activeSelf)
        {
            _infoImage.gameObject.SetActive(true);
        }

        SetInfoAreaImageContainerVisible(_infoImage, true);
        _infoImage.enabled = true;
        EnchantIconLoader.ApplyIcon(_infoImage, _selectedData.ImageKey);
        SetInfoAreaLevelText(_infoImage, _selectedData.Level);
    }

    private void ClearInfoAreaIconAndLevel(GameObject _panel, string _imageObjectName)
    {
        Image _infoImage = FindImageInPanel(_panel, _imageObjectName);
        if (_infoImage == null)
        {
            return;
        }

        _infoImage.sprite = null;
        _infoImage.enabled = false;
        _infoImage.gameObject.SetActive(false);
        SetInfoAreaImageContainerVisible(_infoImage, false);
        TMP_Text _levelText = _infoImage.GetComponentInChildren<TMP_Text>(true);
        if (_levelText != null)
        {
            _levelText.text = string.Empty;
            _levelText.enabled = false;
            _levelText.gameObject.SetActive(false);
        }
    }

    private void SetInfoAreaImageContainerVisible(Image _infoImage, bool _isVisible)
    {
        if (_infoImage == null || _infoImage.transform.parent == null)
        {
            return;
        }

        Transform _parent = _infoImage.transform.parent;
        if (_parent.name == "Image")
        {
            _parent.gameObject.SetActive(_isVisible);
        }
    }

    private Image FindImageInPanel(GameObject _panel, string _imageObjectName)
    {
        if (_panel == null)
        {
            return null;
        }

        Image[] _images = _panel.GetComponentsInChildren<Image>(true);
        for (int _index = 0; _index < _images.Length; _index++)
        {
            Image _image = _images[_index];
            if (_image != null && _image.gameObject.name == _imageObjectName)
            {
                return _image;
            }
        }

        return null;
    }

    private void SetInfoAreaLevelText(Image _infoImage, int _level)
    {
        TMP_Text _levelText = _infoImage.GetComponentInChildren<TMP_Text>(true);
        if (_levelText == null)
        {
            return;
        }

        if (!_levelText.gameObject.activeSelf)
        {
            _levelText.gameObject.SetActive(true);
        }

        _levelText.enabled = true;
        _levelText.SetText("Lv.{0}", Mathf.Max(1, _level));
    }

    private void SetInfoAreaTagText(GameObject _panel, EnchantDisplayData _selectedData)
    {
        TMP_Text _tagText = FindInfoAreaDirectText(_panel);
        if (_tagText == null)
        {
            return;
        }

        if (!_tagText.gameObject.activeSelf)
        {
            _tagText.gameObject.SetActive(true);
        }

        _tagText.enabled = true;
        _tagText.text = BuildInfoAreaText(_selectedData);
    }

    private void ClearInfoAreaTagText(GameObject _panel)
    {
        TMP_Text _tagText = FindInfoAreaDirectText(_panel);
        if (_tagText != null)
        {
            _tagText.text = string.Empty;
            _tagText.enabled = false;
            _tagText.gameObject.SetActive(false);
        }
    }

    private TMP_Text FindInfoAreaDirectText(GameObject _panel)
    {
        Transform _infoArea = FindChildByName(_panel != null ? _panel.transform : null, "InfoArea");
        if (_infoArea == null)
        {
            return null;
        }

        for (int _index = 0; _index < _infoArea.childCount; _index++)
        {
            Transform _child = _infoArea.GetChild(_index);
            if (_child != null && _child.name == "Text (TMP)")
            {
                return _child.GetComponent<TMP_Text>();
            }
        }

        return null;
    }

    private Transform FindChildByName(Transform _root, string _childName)
    {
        if (_root == null)
        {
            return null;
        }

        if (_root.name == _childName)
        {
            return _root;
        }

        for (int _index = 0; _index < _root.childCount; _index++)
        {
            Transform _foundChild = FindChildByName(_root.GetChild(_index), _childName);
            if (_foundChild != null)
            {
                return _foundChild;
            }
        }

        return null;
    }

    private string BuildGuideText(string _description)
    {
        if (string.IsNullOrWhiteSpace(_description))
        {
            return _description;
        }

        string[] _lines = _description.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        List<string> _guideLines = new List<string>();
        for (int _index = 0; _index < _lines.Length; _index++)
        {
            string _lineWithoutTags = Regex.Replace(_lines[_index], @"#[^\s#]+", string.Empty);
            _lineWithoutTags = Regex.Replace(_lineWithoutTags, @"[ \t]{2,}", " ").Trim();
            if (string.IsNullOrWhiteSpace(_lineWithoutTags))
            {
                continue;
            }

            _guideLines.Add(_lineWithoutTags);
        }

        return string.Join("\n", _guideLines).Trim();
    }

    private string BuildInfoAreaText(EnchantDisplayData _selectedData)
    {
        List<string> _tagLines = GetTagLines(_selectedData.Description);
        if (_tagLines.Count <= 0)
        {
            return _selectedData.Name;
        }

        string _tagText = string.Join("  ", _tagLines);
        return $"{_selectedData.Name}\n\n{_tagText}";
    }

    private List<string> GetTagLines(string _description)
    {
        List<string> _tagLines = new List<string>();
        if (string.IsNullOrWhiteSpace(_description))
        {
            return _tagLines;
        }

        MatchCollection _tagMatches = Regex.Matches(_description, @"#([^\s#]+)");
        for (int _index = 0; _index < _tagMatches.Count; _index++)
        {
            string _tag = _tagMatches[_index].Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(_tag))
            {
                continue;
            }

            _tagLines.Add(_tag);
        }

        return _tagLines;
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
