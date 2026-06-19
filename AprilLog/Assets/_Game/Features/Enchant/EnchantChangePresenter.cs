// 설명 : 인챈트 한도 초과 시, 기존 인챈트를 버리고 새 인챈트를 얻는 플로우 제어

using System;
using System.Collections.Generic;
using UnityEngine;

public class EnchantChangePresenter
{
    private readonly IEnchantChangeView _view;
    private readonly EnchantModel _model;
    private readonly EnchantUIModel _uiModel;
    private readonly ScreenNavigator _navigator;
    private  EnchantListPresenter _listPresenter;

    // 대기 중인(방금 뽑아서 얻으려고 하는) 인챈트 데이터
    private EnchantCandidate _pendingEnchant; 

    public EnchantChangePresenter(IEnchantChangeView view, EnchantModel model, EnchantUIModel uiModel, ScreenNavigator navigator)
    {
        _view = view;
        _model = model;
        _uiModel = uiModel;
        _navigator = navigator;

        // View의 클릭 이벤트 구독
        _view.OnDiscardConfirmed += HandleChange;
        _view.OnCancelClicked += HandleCancel;
    }

    public void Dispose()
    {
        _view.OnDiscardConfirmed -= HandleChange;
        _view.OnCancelClicked -= HandleCancel;
    }
    
    
    /// <summary>
    /// 이전 팝업(SelectPresenter)에서 한도 초과 시 호출
    /// </summary>
    /// <param name="newEnchant">한도가 초과 되어 처리를 결정해야 되는 인첸트</param>
    public void OpenChangePopup(EnchantCandidate newEnchant)
    {
        _pendingEnchant = newEnchant;
        _navigator.ShowEnchantChange(); 
        RefreshView();
    }

    private void RefreshView()
    {
        // 새로 얻을 인챈트 세팅
        var newData = new EnchantDisplayData
        {
            EnchantId = _pendingEnchant.Specific_ID,
            Level = _pendingEnchant.Level,
            TypeLabel = _pendingEnchant.Type == EnchantType.Skill ? "스킬" : "스탯",
            // ToDo : 이하 두 변수는 차후 번역 데이터 연결 할 것
            Name = $"Name ID: {_pendingEnchant.Name_ID}",
            Description = _pendingEnchant.Type == EnchantType.Skill ? 
                          $"Description ID: {_pendingEnchant.SkillData.Skill_Descrip}" : $"Description ID: {_pendingEnchant.StatData.StatDescrip}",
            // ToDo : 차후 이미지 컬럼 변경 가능성 있으며, 이미지 불러오는 방법 결정 되면 수정해야됨
            ImageKey = _pendingEnchant.Type == EnchantType.Skill ? 
                        $"{_pendingEnchant.SkillData.SkillIcon_ID}" : $"{_pendingEnchant.StatData.Image_ID}" 
        };
        _view.SetNewEnchantInfo(newData);
        
        if (_listPresenter == null)
        {
            _listPresenter = _view.GetEnchantList();
        }
        
        // 현재 보유 중인 인챈트 목록 세팅 (같은 종류만 버릴 수 있도록 필터링)
        var ownedList = new List<EnchantDisplayData>();

        switch (_pendingEnchant.Type)
        {
            case EnchantType.Skill:
                ownedList = _uiModel.OwnedSkillList;
                break;
            case EnchantType.Stat:
                ownedList = _uiModel.OwnedStatList;
                break;
        }

        _view.SetOwnedEnchantList(ownedList);
    }

    // ---------- 유저 클릭 처리 ----------
    private void HandleChange(int discardNameId)
    {
        switch (_pendingEnchant.Type)
        {
            case EnchantType.Skill:
                _model.RemoveSkill(discardNameId);
                _model.AcquireSkill(_pendingEnchant.Name_ID, _pendingEnchant.SkillData.SkillGroup_ID);
                break;
            case EnchantType.Stat:
                _model.RemoveStat(discardNameId);
                _model.AcquireStat(_pendingEnchant.Name_ID, _pendingEnchant.StatData.StatGroup_ID);
                break;
        }
        
        _navigator.OnCloseButtonClick(); 
    }

    private void HandleCancel()
    {
        _navigator.OnCloseButtonClick();
    }
}