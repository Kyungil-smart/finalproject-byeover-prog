// 생성자 : 김영찬
// 인첸트 목록 UI를 구동하기 위한 스크립트 -- Presenter

// 2차 수정자 : 조규민
// 수정 내용 : 보유 인챈트 선택 시 상세 정보 테이블을 갱신

using System;
using System.Collections.Generic;
using UnityEngine;

public class EnchantListPresenter
{
    private readonly EnchantUIModel _model;
    private readonly IEnchantListView _view;
    private bool _viewEnable;
    

    public EnchantListPresenter(EnchantUIModel model, IEnchantListView view)
    {
        _model = model;
        _view = view;

        _view.OnEnabled += HandleOnViewEnable;
        _view.OnSkillEnchantSelected += HandleSkillEnchantSelected;
        _view.OnStatEnchantSelected += HandleStatEnchantSelected;
        _model.OnSkillListChanged += SetSkillList;
        _model.OnStatListChanged += SetStatList;
    }

    private void HandleOnViewEnable(bool _isEnabled)
    {
        _viewEnable = _isEnabled;
        SetView();
    }

    private void SetView()
    {
        if(!_viewEnable) return;
        SetSkillList();
        SetStatList();
    }

    private void SetSkillList()
    {
        if(!_viewEnable) return;
        _view.SetOwnedSkillList(_model.OwnedSkillList);

        if (_model.OwnedSkillList == null || _model.OwnedSkillList.Count <= 0)
        {
            _view.ClearSelectedSkillEnchantInfo();
            return;
        }

        _view.SetSelectedSkillEnchantInfo(_model.OwnedSkillList[0]);
    }

    private void SetStatList()
    {
        if(!_viewEnable) return;
        _view.SetOwnedStatList(_model.OwnedStatList);

        if (_model.OwnedStatList == null || _model.OwnedStatList.Count <= 0)
        {
            _view.ClearSelectedStatEnchantInfo();
            return;
        }

        _view.SetSelectedStatEnchantInfo(_model.OwnedStatList[0]);
    }

    private void HandleSkillEnchantSelected(EnchantDisplayData _selectedData)
    {
        if(!_viewEnable) return;
        _view.SetSelectedSkillEnchantInfo(_selectedData);
    }

    private void HandleStatEnchantSelected(EnchantDisplayData _selectedData)
    {
        if(!_viewEnable) return;
        _view.SetSelectedStatEnchantInfo(_selectedData);
    }
}
