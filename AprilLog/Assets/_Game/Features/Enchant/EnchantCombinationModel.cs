using System;
using System.Collections.Generic;
using UnityEngine;

public class EnchantCombinationModel : MonoBehaviour
{
    // ---------- 직렬화 ----------
    [SerializeField] private EnchantModel _model;
    
    // ---------- 데이터 ----------
    private Dictionary<int, FusionEnchantData> _fusionData; // nameId, Data
    public Dictionary<int, FusionEnchantData> FusionData => _fusionData;
    
    // ---------- private  ----------
    private SpellRepo _repo;
    
    // ---------- 이벤트  ----------
    private event Action OnCombinationChanged;
    
    // ---------- 초기화 ----------
    public void InitCombinationModel()
    {
        _fusionData = new ();
        _repo = DataManager.Instance.SpellRepo;
        
        // 머지 후 씬 미배선 방어: 역참조(EnchantModel)가 안 꽂혀 있으면 같은 오브젝트에서 찾는다.
        if (_model == null) _model = GetComponent<EnchantModel>();
        if (_model == null)
        {
            Debug.LogError("[EnchantFusionModel] Enchant Model Not Found. Init Failed.");
            return; // 그래도 없으면 이벤트 구독 스킵 (크래시 방지)
        }
        
        _model.OnSkillAcquired += HandleSkillAcquired;
        _model.OnSkillLevelUp += HandleSkillLevelUp;
        _model.OnSkillRemoved += HandleSkillRemoved;
    }

    public void Discard()
    {
        if(_model != null) return;
        
        _model.OnSkillAcquired -= HandleSkillAcquired;
        _model.OnSkillLevelUp -= HandleSkillLevelUp;
        _model.OnSkillRemoved -= HandleSkillRemoved;
    }
    
    // ---------- 이벤트 핸들러 ----------
    private void HandleSkillAcquired(int nameId, int level)
    {
        if (!TryGetCombinationData(nameId, level, out var data)) return;
        
        SetCombinationData(nameId, data);
        
        OnCombinationChanged?.Invoke();
    }

    private void HandleSkillLevelUp(int nameId, int level)
    {
        if (_fusionData.TryGetValue(nameId, out var data))
        {
            data.LevelUp();
        }
        else if (TryGetCombinationData(nameId, level, out var temp))
        {
            SetCombinationData(nameId, temp);
        }
        
        OnCombinationChanged?.Invoke();
    }
    
    private void HandleSkillRemoved(int nameId)
    {
        _fusionData.Remove(nameId);
        OnCombinationChanged?.Invoke();
    }
    
    // ---------- 보조 함수 ----------
    private bool TryGetCombinationData(int nameId, int level, out SkillTableData data)
    {
        var temp = _repo.GetSkillChainByName(EnchantModel.GROUP_COMBINATION_SKILL, nameId);
        if (temp == null)
        {
            data = null;
            return false;
        }

        var temp2 = temp.GetNextLevelData(level - 1);
        if (temp2 == null)
        {
            data = null;
            return false;
        }

        data = temp2;
        return true;
    }

    private void SetCombinationData(int nameId, SkillTableData data)
    {
        if(!_fusionData.TryAdd(nameId, new FusionEnchantData(
               data.Skill_ID, Mathf.RoundToInt(data.RequiredValue_1), Mathf.RoundToInt(data.RequiredValue_2),
               Mathf.RoundToInt(data.RequiredValue_3), data.SkillIcon_ID)))
        {
            Debug.LogError($"[EnchantFusionModel] This skill Already Exists. Name Id = {data.Name}");
        }
    }
}
