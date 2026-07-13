// 설명 : 인챈트 한도 초과 시, 기존 인챈트를 버리고 새 인챈트를 얻는 플로우 제어

// 수정자 : 김영찬
// 수정 내용 : 번역 데이터 연결

// 2차 수정자 : 조규민
// 수정 내용 : 인챈트 교체 완료 확인 팝업에서 확인한 뒤 창이 닫히도록 교체 흐름 분리
// 수정 내용 : 스탯 인챈트가 교체 슬롯의 1, 3, 5번 위치를 사용하도록 인챈트 종류를 View에 전달

using System;
using System.Collections.Generic;
using UnityEngine;

public class EnchantChangePresenter
{
    private readonly IEnchantChangeView _view;
    private readonly EnchantModel _model;
    private readonly EnchantUIModel _uiModel;
    private readonly ScreenNavigator _navigator;
    private EnchantListPresenter _listPresenter;
    private readonly LocalizationManager _localizationManager;

    // 대기 중인(방금 뽑아서 얻으려고 하는) 인챈트 데이터
    private EnchantCandidate _pendingEnchant; 
    private EnchantDisplayData _pendingEnchantDisplayData;

    public EnchantChangePresenter(IEnchantChangeView view, EnchantModel model, EnchantUIModel uiModel, ScreenNavigator navigator)
    {
        _view = view;
        _model = model;
        _uiModel = uiModel;
        _navigator = navigator;
        
        _localizationManager = LocalizationManager.Instance;

        // View의 클릭 이벤트 구독
        _view.OnDiscardConfirmed += HandleChange;
        _view.OnCancelClicked += HandleCancel;
        _view.OnChangeCompleteConfirmed += HandleChangeCompleteConfirmed;
    }

    public void Dispose()
    {
        _view.OnDiscardConfirmed -= HandleChange;
        _view.OnCancelClicked -= HandleCancel;
        _view.OnChangeCompleteConfirmed -= HandleChangeCompleteConfirmed;
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
        EnchantDisplayData newData;
        var enchantGroupType = EnchantGroupIDToEnchantGroupTypeMapper.GetEnchantGroupType(
            _pendingEnchant.Type == EnchantType.Skill ?
                _pendingEnchant.SkillData.SkillGroup_ID : _pendingEnchant.StatData.StatGroup_ID);
        var elementalType = TagToElementalMapper.GetElemental(
            _pendingEnchant.Type == EnchantType.Skill ? 
                _pendingEnchant.SkillData.Tag_ID_1 : _pendingEnchant.StatData.Target_2);
        
        if(_localizationManager == null)
        {
            Debug.LogWarning("No localization manager found. No Localization.");
            newData = new EnchantDisplayData
            {
                EnchantId = _pendingEnchant.Specific_ID,
                Level = _pendingEnchant.Level,
                TypeLabel = enchantGroupType,
                // 번역 데이터가 없음으로 ID를 출력함
                Name = $"Name ID: {_pendingEnchant.Name_ID}",
                Description = _pendingEnchant.Type == EnchantType.Skill
                    ? $"Description ID: {_pendingEnchant.SkillData.Skill_Descrip}"
                    : $"Description ID: {_pendingEnchant.StatData.StatDescrip}",
                // ToDo : 차후 이미지 컬럼 변경 가능성 있으며, 이미지 불러오는 방법 결정 되면 수정해야됨
                ImageKey = _pendingEnchant.Type == EnchantType.Skill
                    ? $"{_pendingEnchant.SkillData.SkillIcon_ID}"
                    : $"{_pendingEnchant.StatData.Image_ID}",
                ElementalType = elementalType
            };
        }
        else
        {
            newData = new EnchantDisplayData
            {
                EnchantId = _pendingEnchant.Specific_ID,
                Level = _pendingEnchant.Level,
                TypeLabel = enchantGroupType,
                Name = _localizationManager.Get(_pendingEnchant.Name_ID, LocalizingType.Enchant),
                Description = _pendingEnchant.Type == EnchantType.Skill ? 
                    _localizationManager.Get(_pendingEnchant.SkillData.Skill_Descrip, LocalizingType.Enchant, _pendingEnchant.SkillData.RequiredValue_1) : 
                    _localizationManager.Get(_pendingEnchant.StatData.StatDescrip, LocalizingType.Enchant),
                // ToDo : 차후 이미지 컬럼 변경 가능성 있으며, 이미지 불러오는 방법 결정 되면 수정해야됨
                ImageKey = _pendingEnchant.Type == EnchantType.Skill ? 
                    $"{_pendingEnchant.SkillData.SkillIcon_ID}" : $"{_pendingEnchant.StatData.Image_ID}",
                ElementalType = elementalType
            };
        }
        
        _pendingEnchantDisplayData = newData;
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

        // 추가: 조규민 - 스탯 인챈트가 교체 슬롯의 1, 3, 5번 위치를 사용하도록 인챈트 종류를 View에 전달한다.
        _view.SetOwnedEnchantList(ownedList, _pendingEnchant.Type);
    }

    // ---------- 유저 클릭 처리 ----------
    private void HandleChange(int discardNameId)
    {
        EnchantDisplayData _discardData = FindOwnedEnchantDisplayData(discardNameId);

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

        AudioManager.Play(SfxId.EnchantChange);   // SFX 가이드 10: 보유 인챈트 교체
        
        _view.ShowChangeCompletePopup(_discardData, _pendingEnchantDisplayData);
    }

    private void HandleCancel()
    {
        _navigator.OnCloseButtonClick();
    }

    private void HandleChangeCompleteConfirmed()
    {
        _navigator.OnCloseButtonClick();
    }

    private EnchantDisplayData FindOwnedEnchantDisplayData(int _discardNameId)
    {
        List<EnchantDisplayData> _ownedList = _pendingEnchant.Type == EnchantType.Skill
            ? _uiModel.OwnedSkillList
            : _uiModel.OwnedStatList;

        if (_ownedList == null)
        {
            return null;
        }

        for (int _index = 0; _index < _ownedList.Count; _index++)
        {
            EnchantDisplayData _ownedData = _ownedList[_index];
            if (_ownedData == null || _ownedData.EnchantId != _discardNameId)
            {
                continue;
            }

            return _ownedData;
        }

        return null;
    }
}
