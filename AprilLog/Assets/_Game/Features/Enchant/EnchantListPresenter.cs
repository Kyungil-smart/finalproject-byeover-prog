// 생성자 : 김영찬
// 인첸트 목록 UI를 구동하기 위한 스크립트 -- Presenter

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
        _model.OnSkillListChanged += SetSkillList;
        _model.OnStatListChanged += SetStatList;
    }

    private void HandleOnViewEnable(bool isEnabled)
    {
        _viewEnable = isEnabled;
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
    }

    private void SetStatList()
    {
        if(!_viewEnable) return;
        _view.SetOwnedStatList(_model.OwnedStatList);
    }
}
