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

        // 머지 후 씬 미배선 방어: 역참조(EnchantModel)가 안 꽂혀 있으면 같은 오브젝트에서 찾는다.
        if (_model == null) _model = GetComponent<EnchantModel>();
        if (_model == null) return; // 그래도 없으면 이벤트 구독 스킵 (크래시 방지)

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
                    Name = $"Skill_ID: {data.Data.StatName}",
                    Description = $"Description_ID: {data.Data.StatDescrip}",
                    ImageKey = $"{data.Data.Image_ID}"
                });
            }
        }
        
        OnStatListChanged?.Invoke();
    }

    public void Discard()
    {
        if (_model == null) return;
        _model.OnSkillAcquired -= HandleSkillRefresh;
        _model.OnSkillLevelUp -= HandleSkillRefresh;
        _model.OnSkillRemoved -= HandleSkillRefresh;
        _model.OnStatAcquired -= HandleStatRefresh;
        _model.OnStatLevelUp -= HandleStatRefresh;
        _model.OnStatRemoved -= HandleStatRefresh;
    }
}
