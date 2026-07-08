// 생성자 : 김영찬
// 인첸트 교체 UI를 구동하기 위한 스크립트 -- View

// 1차 수정자 : 조규민
// 수정 내용 : 보유 인챈트 목록 복원 후 교체 슬롯이 비거나 버튼 수보다 목록이 적을 때 이전 표시/인덱스 오류가 남지 않도록 수정
// 수정 내용 : 인챈트 교체 팝업의 기존 인챈트 선택 정보 초기화, 선택 강조, 교체 버튼 활성 상태 제어
// 수정 내용 : 교체 완료/취소 확인 팝업을 Inspector 배치 UI로 제어

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnchantChangeView : MonoBehaviour, IEnchantChangeView
{
    [Header("UI Elements")]
    [SerializeField] private Button[] _changeSkillSelectButtons;
    [SerializeField] private EnchantChangeInfoTableUI _afterEnchantChangeInfoTable;

    [Tooltip("교체를 실행하는 버튼")] 
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    
    [Header("번역 대비")]
    [SerializeField] private TextMeshProUGUI _headerText;
    [SerializeField] private TextMeshProUGUI _changeExecuteButtonText;
    [SerializeField] private TextMeshProUGUI _cancelButtonText;

    [Header("교체 완료 확인 팝업")]
    [SerializeField] private GameObject _changeCompletePopupRoot;
    [SerializeField] private Image _changeCompleteBeforeIconImage;
    [SerializeField] private Image _changeCompleteAfterIconImage;
    [SerializeField] private Button _changeCompleteConfirmButton;

    [Header("교체 취소 확인 팝업")]
    [SerializeField] private GameObject _cancelConfirmPopupRoot;
    [SerializeField] private Button _cancelConfirmYesButton;
    [SerializeField] private Button _cancelConfirmNoButton;

    [Header("참조")] 
    [SerializeField] private EnchantListView _listView;
    
    private int _selectedDiscardNameId = -1;
    private EnchantDisplayData _selectedDiscardData;
    private EnchantDisplayData _newEnchantData;
    private EnchantChangeInfoTableUI _beforeEnchantChangeInfoTable;
    private readonly List<EnchantChangeSelectButtonUI> _boundButtonUIs = new List<EnchantChangeSelectButtonUI>();
    
    public event Action<int> OnDiscardConfirmed;
    public event Action OnCancelClicked;
    public event Action OnChangeCompleteConfirmed;

    private void Awake()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(HandleConfirmButtonClicked);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(OpenCancelConfirmPopup);
        }

        if (_changeCompleteConfirmButton != null)
        {
            _changeCompleteConfirmButton.onClick.AddListener(HandleChangeCompleteConfirmed);
        }

        if (_cancelConfirmYesButton != null)
        {
            _cancelConfirmYesButton.onClick.AddListener(HandleCancelConfirmed);
        }

        if (_cancelConfirmNoButton != null)
        {
            _cancelConfirmNoButton.onClick.AddListener(CloseCancelConfirmPopup);
        }

        HideConfirmPopups();
        SetConfirmButtonInteractable(false);
    }

    private void OnDestroy()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(HandleConfirmButtonClicked);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(OpenCancelConfirmPopup);
        }

        if (_changeCompleteConfirmButton != null)
        {
            _changeCompleteConfirmButton.onClick.RemoveListener(HandleChangeCompleteConfirmed);
        }

        if (_cancelConfirmYesButton != null)
        {
            _cancelConfirmYesButton.onClick.RemoveListener(HandleCancelConfirmed);
        }

        if (_cancelConfirmNoButton != null)
        {
            _cancelConfirmNoButton.onClick.RemoveListener(CloseCancelConfirmPopup);
        }

        UnbindSelectButtons();
    }

    // Presenter가 새 인챈트 정보를 줄 때
    public void SetNewEnchantInfo(EnchantDisplayData _newData)
    {
        _newEnchantData = _newData;

        if (_afterEnchantChangeInfoTable == null)
        {
            Debug.LogWarning("[EnchantChangeView] After EnchantChangeInfoTable Not Serialized", this);
            return;
        }

        _afterEnchantChangeInfoTable.SetInfo(_newData);
    }

    // Presenter가 보유 중인 인챈트 목록을 줄 때
    public void SetOwnedEnchantList(List<EnchantDisplayData> _ownedList)
    {
        _selectedDiscardNameId = -1;
        _selectedDiscardData = null;
        HideConfirmPopups();
        SetConfirmButtonInteractable(false);
        UnbindSelectButtons();
        ResolveBeforeInfoTable()?.ClearInfo();

        if(_changeSkillSelectButtons == null)
        {
            Debug.LogWarning("ChangeSkillSelectButtons Not Serialized");
            return;
        }

        for (int _index = 0; _index < _changeSkillSelectButtons.Length; _index++)
        {
            EnchantDisplayData _ownedData = _ownedList != null && _index < _ownedList.Count ? _ownedList[_index] : null;
            Button _button = _changeSkillSelectButtons[_index];
            if (_button == null)
            {
                continue;
            }

            EnchantChangeSelectButtonUI _buttonUI = _button.GetComponent<EnchantChangeSelectButtonUI>();
            if (_buttonUI != null)
            {
                _buttonUI.SetInfo(_ownedData);
                _buttonUI.SetSelected(false);
            }

            _button.interactable = _ownedData != null;
            _button.gameObject.SetActive(_ownedData != null);

            if (_ownedData != null && _buttonUI != null)
            {
                _buttonUI.OnEnchantSelected += SetDiscardName;
                _buttonUI.OnEnchantDisplaySelected += SetDiscardInfo;
                _boundButtonUIs.Add(_buttonUI);
            }
        }
    }
    
    private void SetDiscardName(int _discardNameId)
    {
        _selectedDiscardNameId = _discardNameId;
        SetConfirmButtonInteractable(true);
    }

    private void SetDiscardInfo(EnchantDisplayData _selectedData)
    {
        if (_selectedData == null)
        {
            return;
        }

        _selectedDiscardData = _selectedData;

        EnchantChangeInfoTableUI _infoTable = ResolveBeforeInfoTable();
        if (_infoTable != null)
        {
            _infoTable.SetInfo(_selectedData);
        }

        for (int _index = 0; _index < _boundButtonUIs.Count; _index++)
        {
            EnchantChangeSelectButtonUI _buttonUI = _boundButtonUIs[_index];
            if (_buttonUI == null)
            {
                continue;
            }

            _buttonUI.SetSelected(_buttonUI.EnchantDisplayData == _selectedData);
        }
    }

    private EnchantChangeInfoTableUI ResolveBeforeInfoTable()
    {
        if (_beforeEnchantChangeInfoTable != null)
        {
            return _beforeEnchantChangeInfoTable;
        }

        if (_changeSkillSelectButtons == null)
        {
            return null;
        }

        for (int _index = 0; _index < _changeSkillSelectButtons.Length; _index++)
        {
            Button _button = _changeSkillSelectButtons[_index];
            if (_button == null)
            {
                continue;
            }

            EnchantChangeSelectButtonUI _buttonUI = _button.GetComponent<EnchantChangeSelectButtonUI>();
            if (_buttonUI == null || _buttonUI.InfoTableUI == null)
            {
                continue;
            }

            _beforeEnchantChangeInfoTable = _buttonUI.InfoTableUI;
            return _beforeEnchantChangeInfoTable;
        }

        return null;
    }

    private void SetConfirmButtonInteractable(bool _isInteractable)
    {
        if (_confirmButton == null)
        {
            return;
        }

        _confirmButton.interactable = _isInteractable;
    }

    public void ShowChangeCompletePopup(EnchantDisplayData _discardData, EnchantDisplayData _newData)
    {
        EnchantDisplayData _popupDiscardData = _discardData ?? _selectedDiscardData;
        EnchantDisplayData _popupNewData = _newData ?? _newEnchantData;

        CloseCancelConfirmPopup();
        ApplyPopupIcon(_changeCompleteBeforeIconImage, _popupDiscardData);
        ApplyPopupIcon(_changeCompleteAfterIconImage, _popupNewData);
        SetGameObjectActive(_changeCompletePopupRoot, true);
    }

    private void HandleConfirmButtonClicked()
    {
        if (_selectedDiscardNameId == -1)
        {
            return;
        }

        SetConfirmButtonInteractable(false);
        OnDiscardConfirmed?.Invoke(_selectedDiscardNameId);
    }

    private void HandleChangeCompleteConfirmed()
    {
        HideChangeCompletePopup();
        OnChangeCompleteConfirmed?.Invoke();
    }

    private void HandleCancelConfirmed()
    {
        CloseCancelConfirmPopup();
        OnCancelClicked?.Invoke();
    }

    private void OpenCancelConfirmPopup()
    {
        HideChangeCompletePopup();
        SetGameObjectActive(_cancelConfirmPopupRoot, true);
    }

    private void CloseCancelConfirmPopup()
    {
        SetGameObjectActive(_cancelConfirmPopupRoot, false);
    }

    private void HideChangeCompletePopup()
    {
        SetGameObjectActive(_changeCompletePopupRoot, false);
    }

    private void HideConfirmPopups()
    {
        HideChangeCompletePopup();
        CloseCancelConfirmPopup();
    }

    private void ApplyPopupIcon(Image _image, EnchantDisplayData _data)
    {
        if (_image == null)
        {
            return;
        }

        if (_data == null)
        {
            _image.sprite = null;
            _image.enabled = false;
            return;
        }

        _image.enabled = true;
        EnchantIconLoader.ApplyIcon(_image, _data.ImageKey);
    }

    private void SetGameObjectActive(GameObject _targetObject, bool _isActive)
    {
        if (_targetObject == null)
        {
            return;
        }

        _targetObject.SetActive(_isActive);
    }

    private void UnbindSelectButtons()
    {
        for (int _index = 0; _index < _boundButtonUIs.Count; _index++)
        {
            EnchantChangeSelectButtonUI _buttonUI = _boundButtonUIs[_index];
            if (_buttonUI == null)
            {
                continue;
            }

            _buttonUI.OnEnchantSelected -= SetDiscardName;
            _buttonUI.OnEnchantDisplaySelected -= SetDiscardInfo;
        }

        _boundButtonUIs.Clear();
    }

    // EnchantListPresenter 받아옴 (Presenter는 유니티를 모름으로 대신 컴포넌트를 받아옴)
    public EnchantListPresenter GetEnchantList()
    {
        if (_listView == null)
        {
            Debug.LogWarning("ListView Not Set");
            return null;
        }

        if (!_listView.IsInitialized)
        {
            Debug.LogWarning("ListView Not Initialized");
            return null;
        }

        return _listView.Presenter;
    }
}
