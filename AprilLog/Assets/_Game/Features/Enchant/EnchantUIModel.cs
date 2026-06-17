using System;
using System.Collections.Generic;
using UnityEngine;

public class EnchantUIModel : MonoBehaviour
{
    // ---------- 직렬화 ----------
    [SerializeField] private EnchantModel _model;
    
    // ---------- UI용으로 가공된 정보 ----------
    public List<EnchantDisplayData> OwnedSkillList { get ; private set; }
    public List<EnchantDisplayData> OwnedStatList { get ; private set; }
    
    // ---------- 이벤트 ----------
    public event Action OnSkillListChanged;
    public event Action OnStatListChanged;
    
    // ---------- 초기화 ----------
    public void InitUIModel()
    {
        OwnedSkillList = new ();
        OwnedStatList = new ();
        
        _model.OnSkillAcquired += HandleSkillRefresh;
        _model.OnSkillLevelUp += HandleSkillRefresh;
        _model.OnSkillRemoved += HandleSkillRefresh;
        _model.OnStatAcquired += HandleStatRefresh;
        _model.OnStatLevelUp += HandleStatRefresh;
        _model.OnStatRemoved += HandleStatRefresh;
    }
    
    // ---------- 이벤트 핸들러 ----------
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
        
        OnSkillListChanged?.Invoke();
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
        
        OnStatListChanged?.Invoke();
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
