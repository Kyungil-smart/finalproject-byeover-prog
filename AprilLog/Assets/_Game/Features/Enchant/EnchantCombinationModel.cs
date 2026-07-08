// 작성자 : 김영찬
// 내용 : 인첸트 모델의 스킬 선택 목록을 보고 해당 데이터 중 조합 스킬에 대한 처리를 담당함

// 수정자 : 조규민
// 수정 내용 : 이어하기 복원 중 EnchantModel 재초기화 시 조합 인챈트 이벤트가 중복 구독되지 않도록 수정
// 수정 내용 : 로비 복귀 후 이어하기 복원 시 조합 인챈트 아이콘 표시 데이터가 다시 채워지도록 보유 스킬 기준 재구성 경로 추가

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
    public event Action OnCombinationChanged;
    
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
        
        UnbindModelEvents();
        _model.OnSkillAcquired += HandleSkillAcquired;
        _model.OnSkillLevelUp += HandleSkillLevelUp;
        _model.OnSkillRemoved += HandleSkillRemoved;
    }
  
    public void RebuildFromOwnedSkills()
    {
        if (_fusionData == null)
        {
            _fusionData = new Dictionary<int, FusionEnchantData>();
        }
        else
        {
            _fusionData.Clear();
        }

        if (_model == null)
        {
            _model = GetComponent<EnchantModel>();
        }

        if (_repo == null && DataManager.Instance != null)
        {
            _repo = DataManager.Instance.SpellRepo;
        }

        if (_model == null || _repo == null)
        {
            OnCombinationChanged?.Invoke();
            return;
        }

        foreach (var _pair in _model.OwnedSkills)
        {
            AcquiredSkillData _ownedSkill = _pair.Value;
            if (_ownedSkill == null)
            {
                continue;
            }

            if (_ownedSkill.GroupID != EnchantModel.GROUP_COMBINATION_SKILL)
            {
                continue;
            }

            SkillTableData _skillData = _ownedSkill.Data;
            if (_skillData == null && TryGetCombinationData(_pair.Key, _ownedSkill.Level, out SkillTableData _restoredData))
            {
                _skillData = _restoredData;
            }

            if (_skillData == null)
            {
                continue;
            }

            SetCombinationData(_pair.Key, _skillData);
        }

        OnCombinationChanged?.Invoke();
    }

    public void Discard()
    {
        if(_model == null)
        {
            return;
        }

        UnbindModelEvents();
    }

    private void UnbindModelEvents()
    {
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
