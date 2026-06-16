// 생성자 : 김영찬
// 인첸트 목록 UI를 구동하기 위한 스크립트 -- Presenter

using System.Collections.Generic;
using UnityEngine;

public class EnchantListPresenter
{
    private readonly EnchantModel _model;
    private readonly IEnchantListView _view;
    public List<EnchantDisplayData> OwnedSkillList = new ();
    public List<EnchantDisplayData> OwnedStatList = new ();
    
    public EnchantListPresenter(EnchantModel model, IEnchantListView view)
    {
        _model = model;
        _view = view;

        _model.OnSkillAcquired += HandleSkillRefresh;
        _model.OnSkillLevelUp += HandleSkillRefresh;
        _model.OnSkillRemoved += HandleSkillRefresh;
        _model.OnStatAcquired += HandleStatRefresh;
        _model.OnStatLevelUp += HandleStatRefresh;
        _model.OnStatRemoved += HandleStatRefresh;
    }

    private void HandleSkillRefresh(int nameId, int level)
    {
        RefreshSkillView();
    }

    private void HandleSkillRefresh(int nameId)
    {
        RefreshSkillView();
    }
    
    private void HandleStatRefresh(int nameId, int level)
    {
        RefreshStatView();
    }

    private void HandleStatRefresh(int nameId)
    {
        RefreshStatView();
    }
    
    private void RefreshSkillView()
    {
        OwnedSkillList.Clear();

        if (_model.OwnedSkills.Count > 0)
        {
            foreach (var data in _model.OwnedSkills.Values)
            {
                OwnedSkillList.Add(new EnchantDisplayData
                {
                    EnchantId = data.Data.Skill_ID,
                    Level = data.Data.Level,
                    TypeLabel = "스킬",
                    Name = $"Skill_ID: {data.Data.Name}",
                    Description = $"Description_ID: {data.Data.Skill_Descrip}",
                    ImageKey = $"{data.Data.SkillIcon_ID}"
                });
            }
        }
        _view.SetOwnedSkillList(OwnedSkillList);
    }

    private void RefreshStatView()
    {
        OwnedStatList.Clear();
        
        if (_model.OwnedStats.Count > 0)
        {
            foreach (var data in _model.OwnedStats.Values)
            {
                OwnedStatList.Add(new EnchantDisplayData
                {
                    EnchantId = data.Data.StatEnchant_ID,
                    Level = data.Data.StatLevel,
                    TypeLabel = "스텟",
                    Name = $"Skill_ID: {data.Data.Stat_Name}",
                    Description = $"Description_ID: {data.Data.Stat_Descrip}",
                    ImageKey = $"{data.Data.Image_ID}"
                });
            }
        }
        _view.SetOwnedStatList(OwnedStatList);
    }

    public void Discard()
    {
        _model.OnSkillAcquired -= HandleSkillRefresh;
        _model.OnSkillLevelUp -= HandleSkillRefresh;
        _model.OnSkillRemoved -= HandleSkillRefresh;
        _model.OnStatAcquired -= HandleStatRefresh;
        _model.OnStatLevelUp -= HandleStatRefresh;
        _model.OnStatRemoved -= HandleStatRefresh;
    }
}
