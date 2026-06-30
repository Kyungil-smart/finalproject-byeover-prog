// 생성자 : 김영찬
// 인첸트 교체 UI를 구동하기 위한 스크립트 -- View

// 수정자 : 조규민
// 수정 내용 : 보유 인챈트 목록 복원 후 교체 슬롯이 비거나 버튼 수보다 목록이 적을 때 이전 표시/인덱스 오류가 남지 않도록 수정

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

    [Header("참조")] 
    [SerializeField] private EnchantListView _listView;
    
    private int _selectedDiscardNameId = -1;
    
    public event Action<int> OnDiscardConfirmed;
    public event Action OnCancelClicked;

    private void Awake()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(() => 
            {
                if (_selectedDiscardNameId != -1)
                {
                    OnDiscardConfirmed?.Invoke(_selectedDiscardNameId);
                }
            });
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(() => OnCancelClicked?.Invoke());
        }
    }

    // Presenter가 새 인챈트 정보를 줄 때
    public void SetNewEnchantInfo(EnchantDisplayData newData)
    {
        _afterEnchantChangeInfoTable.SetInfo(newData);
    }

    // Presenter가 보유 중인 인챈트 목록을 줄 때
    public void SetOwnedEnchantList(List<EnchantDisplayData> ownedList)
    {
        _selectedDiscardNameId = -1;

        if(_changeSkillSelectButtons == null)
        {
            Debug.LogWarning("ChangeSkillSelectButtons Not Serialized");
            return;
        }

        for (int i = 0; i < _changeSkillSelectButtons.Length; i++)
        {
            EnchantDisplayData _ownedData = ownedList != null && i < ownedList.Count ? ownedList[i] : null;
            Button _button = _changeSkillSelectButtons[i];
            if (_button == null)
            {
                continue;
            }

            EnchantChangeSelectButtonUI _buttonUI = _button.GetComponent<EnchantChangeSelectButtonUI>();
            if (_buttonUI != null)
            {
                _buttonUI.OnEnchantSelected -= SetDiscardName;
                _buttonUI.SetInfo(_ownedData);
            }

            _button.interactable = _ownedData != null;
            _button.gameObject.SetActive(_ownedData != null);

            if (_ownedData != null && _buttonUI != null)
            {
                _buttonUI.OnEnchantSelected += SetDiscardName;
            }
        }
    }
    
    private void SetDiscardName(int discard)
    {
        _selectedDiscardNameId = discard;
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
