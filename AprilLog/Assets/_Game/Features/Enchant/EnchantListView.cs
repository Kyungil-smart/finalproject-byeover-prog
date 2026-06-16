// 생성자 : 김영찬
// 인첸트 목록 UI를 구동하기 위한 스크립트 -- View

using System;
using System.Collections.Generic;
using Firebase.AppCheck;
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

    [Header("Skill Panel")]
    [Tooltip("인첸트 변경때 사용한 버튼과 구성이 동일")]
    [SerializeField] Button[] _skillChangeButtons;
    
    [Header("Stat Panel")]
    [Tooltip("인첸트 변경때 사용한 버튼과 구성이 동일")]
    [SerializeField] Button[] _statChangeButtons;

    [Header("참조")] 
    [Tooltip("옵션 버튼의 활성화를 위함 : 스킬 선택창에서 넘어오면 이 UI가 활성화 > 정지 버튼을 눌러서 진입한것이 아니게 됨")]
    [SerializeField] GameObject _skillSelectUI;
    
    private EnchantListPresenter _presenter;
    public EnchantListPresenter Presenter => _presenter;
    
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;
    
    private void OnEnable()
    {
        ToggleOptionButtonSet();
        OnSkillSelectButtonClick();
    }

    private void OnDestroy()
    {
        _presenter.Discard();
    }

    public void InitializePresenter(EnchantModel model)
    {
        if (!_isInitialized)
        {
            _presenter = new EnchantListPresenter(model, this);
            _isInitialized = true;
        }
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
        _skillSelectButton.interactable = false;
        _statSelectButton.interactable = true;
        _skillPanel.SetActive(true);
        _statPanel.SetActive(false);
    }

    public void OnStatSelectButtonClick()
    {
        _skillSelectButton.interactable = true;
        _statSelectButton.interactable = false;
        _skillPanel.SetActive(false);
        _statPanel.SetActive(true);
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
