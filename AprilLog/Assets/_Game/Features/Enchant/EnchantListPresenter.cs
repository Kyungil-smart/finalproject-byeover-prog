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
        var spellRepo = DataManager.Instance.SpellRepo;

        if (_model.OwnedSkills.Count > 0)
        {
            foreach (var group in spellRepo.GetAllSkillGroups().Values)
            {
                foreach (var chain in group.SkillNameChainData.Values)
                {
                    if (_model.HasSkill(chain.Name_ID))
                    {
                        int currentLv = _model.GetSkillLevel(chain.Name_ID);
                        OwnedSkillList.Add(new EnchantDisplayData
                        {
                            EnchantId = chain.Name_ID, 
                            Level = currentLv, 
                            TypeLabel = "스킬",
                            Name = $"NameID: {chain.Name_ID}",
                            Description = $"Description ID: {chain.LevelDataMap[currentLv].Skill_Descrip}",
                            ImageKey = $"{chain.LevelDataMap[currentLv].SkillIcon_ID}"
                        });
                    }
                }
            }
        }
        _view.SetOwnedSkillList(OwnedSkillList);
    }

    private void RefreshStatView()
    {
        OwnedStatList.Clear();
        var spellRepo = DataManager.Instance.SpellRepo;
        
        if (_model.OwnedStats.Count > 0)
        {
            foreach (var group in spellRepo.GetAllStatGroups().Values)
            {
                foreach (var chain in group.StatNameChainData.Values)
                {
                    if (_model.HasStat(chain.Stat_Name_ID))
                    {
                        int currentLv = _model.GetStatLevel(chain.Stat_Name_ID);
                        OwnedStatList.Add(new EnchantDisplayData
                        {
                            EnchantId = chain.Stat_Name_ID, 
                            Level = currentLv, 
                            TypeLabel = "스텟",
                            Name = $"StatID: {chain.Stat_Name_ID}",
                            Description = $"Description ID: {chain.LevelDataMap[currentLv].Stat_Descrip}",
                            ImageKey = $"{chain.LevelDataMap[currentLv].Image_ID}"
                        });
                    }
                }
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
