// 수정자 : 조규민
// 수정 내용 : 이어하기 복원 중 EnchantModel 재초기화 시 스탯 인챈트 이벤트가 중복 구독되지 않도록 수정

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnchantCalculator : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private EnchantModel _enchantModel;

    [Header("크리티컬 기본 배율")]
    [SerializeField][Range(1f,5f)] private float _baseCritDamageRate = 1.4f;
    
    // ---------- private ----------
    private SpellRepo _repo;
    private Dictionary<int, int> _enableStatList; // name, Level;
    private bool isInitialized;
    
    // ---------- 이벤트 ----------
    
    // ---------- Const ----------
    // DamageCalculate
    private const string DAMAGE = "Dmg";
    private const string CRITCAL_RATE = "CriticalRate";
    private const string CRITCAL_DAMAGE = "CriticalDamage";
    
    // ProjectileAddCalculate
    private const string ADD_PROJECTILE = "ActivePlusCount";
    private const string PROJECTILE_GAP  = "PelletGap";
    private const string PROJECTILE_DAMAGE_REDUCE = "subPelletDmg";
    private const int PROJECTILE_TAG = 20;
    
    // SkillAreaExtenstionCalculate
    private const string X_LENGTH_EXTENSION_RATE = "HitSize_X";
    private const string Y_LENGTH_EXTENSION_RATE = "HitSize_Y";
    
    // EffectCalculate
    private const string VALUE = "Main_Value";
    private const string INTERVAL = "Interval";
    private const string DURATION = "Duration";
    private const string KNOCK_BACK = "KnockBack";
    private const string STATUS_UP = "StatusUP";
    
    // 스텟 계산
    private const string HP = "MaxHP";
    private const string ATTACK = "Attack";
    private const string ATTACK_SPEED = "BaseAttackSpeed";
    
    // ---------- 초기화 ----------
    public void InitCalculator()
    {
        _repo = DataManager.Instance.SpellRepo;
        _enableStatList = new ();
        
        // 머지 후 씬 미배선 방어: 역참조(EnchantModel)가 안 꽂혀 있으면 같은 오브젝트에서 찾는다.
        if (_enchantModel == null) _enchantModel = GetComponent<EnchantModel>();
        if (_enchantModel == null)
        {
            Debug.LogError("[EnchantCalculator] Enchant Model Not Found. Init Failed.");
            return; // 그래도 없으면 이벤트 구독 스킵 (크래시 방지)
        }

        UnbindModelEvents();
        _enchantModel.OnStatAcquired += HandleStatAcquired;
        _enchantModel.OnStatLevelUp += HandleStatLevelUp;
        _enchantModel.OnStatRemoved += HandleStatRemoved;
        
        // 머지 후 씬 미배선 방어: 역참조(PlayerModel)가 안 꽂혀 있으면 하이어라키에서 찾는다.
        if (_playerModel == null) _playerModel = FindFirstObjectByType<PlayerModel>();
        if (_playerModel == null)
        {
            Debug.LogError("[EnchantCalculator] Player Model Not Found. Init Failed.");
            return; // 그래도 없으면 이벤트 구독 스킵 (크래시 방지)
        }
        
        isInitialized = true;
    }

    public void Discard()
    {
        if (_enchantModel == null)
        {
            return;
        }

        UnbindModelEvents();
    }

    private void UnbindModelEvents()
    {
        _enchantModel.OnStatAcquired -= HandleStatAcquired;
        _enchantModel.OnStatLevelUp -= HandleStatLevelUp;
        _enchantModel.OnStatRemoved -= HandleStatRemoved;
    }
    
    // ---------- 스킬 계산 ----------
    /// <summary>
    /// 데미지 계산
    /// </summary>
    /// <param name="skillId">계산을 실행할 스킬의 ID</param>
    /// <returns>BaseDamage</returns>
    public int DamageCalculate(int skillId)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[EnchantCalculator] Not Initialized. All DamageCalculate result set 0.");
            return 0;
        }
        
        var data = _repo.GetSkillData(skillId);
        if (data == null)
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the Skill data : {skillId}. All DamageCalculate result set 0.");
            return 0;
        }
        
        // Base_Damage = [ ATK x ( 1 + Stat_ATK_Enchant / 100) x (1 + Skill_Enchant/100) x (1+ Group_Bonus / 100) x CriticalModifier]
        int attack = _playerModel.Attack;
        float critRate = _playerModel.CriticalRate;
        float critDmg = _playerModel.CriticalDamage;
        
        float skillDmgBonus = 1 + (data.Dmg / 100f);
        float skillCritRateBonus = data.CriticalRate;
        
        GetGroupDamageModifier(data, out var groupDmgBonus, out var groupCritRateBonus, out var groupCritDmgBonus);

        float calCritRate = critRate + skillCritRateBonus + groupCritRateBonus;
        float calCritDmg = _baseCritDamageRate * ((100 + (critDmg + groupCritDmgBonus)) / 100f);

        int baseDamage;

        if (Random.Range(0, 1f) <= calCritRate)
        {
            baseDamage = Mathf.FloorToInt(attack * skillDmgBonus * groupDmgBonus * calCritDmg);
        }
        else
        {
            baseDamage = Mathf.FloorToInt(attack * skillDmgBonus * groupDmgBonus);
        }
        
        return baseDamage;
    }

    /// <summary>
    /// 투사체 추가 개수 계산
    /// </summary>
    /// <param name="skillId">계산을 실행할 스킬의 ID</param>
    /// <param name="pelletGap">추가 투사체의 간격 (interval)</param>
    /// <param name="subPelletDmgReduce"></param>
    /// <param name="supPelletDmgReduce">추가 투사체가 원본 투사체에 비해 데미지가 감소하는 정도 (%)</param>
    /// <returns>이 스킬에 적용해야 될 추가 투사체 개수</returns>
    public int ProjectileAddCalculate(int skillId, out float pelletGap, out float subPelletDmgReduce)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[EnchantCalculator] Not Initialized. All ProjectileAddCalculate result set 0.");
            pelletGap = 0;
            subPelletDmgReduce = 0;
            return 0;
        }
        
        var data = _repo.GetSkillData(skillId);
        if (data == null)
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the Skill data : {skillId}. All ProjectileAddCalculate result set 0.");
            pelletGap = 0;
            subPelletDmgReduce = 0;
            return 0;
        }

        GetProjectileAdd(data, out int addProjectile, out pelletGap, out subPelletDmgReduce);
        
        return addProjectile;
    }

    /// <summary>
    /// 투사체 범위 증가 계산
    /// </summary>
    /// <param name="skillId">계산을 실행할 스킬의 ID</param>
    /// <param name="xLengthExtensionRate">기본 히트박스의 X값을 이 값과 곱해야됨</param>
    /// <param name="yLengthExtensionRate">기본 히트박스의 Y값을 이 값과 곱해야됨</param>
    public void SkillAreaExtensionCalculate(int skillId, out float xLengthExtensionRate, out float yLengthExtensionRate)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[EnchantCalculator] Not Initialized. All SkillAreaExtenstionCalculate result set Default.");
            xLengthExtensionRate = 1f;
            yLengthExtensionRate = 1f;
            return;
        }
        
        var data = _repo.GetSkillData(skillId);
        if (data == null)
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the Skill data : {skillId}. All SkillAreaExtenstionCalculate result set Default.");
            xLengthExtensionRate = 1f;
            yLengthExtensionRate = 1f;
            return;
        }
        
        GetSkillAreaExtension(data, out xLengthExtensionRate, out yLengthExtensionRate);
    }
    
    // ---------- 스텟 계산 ----------
    private void StatAcquire(int nameId, int level)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[EnchantCalculator] Not Initialized. All StatAcquire result set Default.");
            return;
        }

        var temp = _repo.GetStatChainByName(EnchantModel.GROUP_MODEL_STAT, nameId);
        if (temp == null)
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the Stat Enchant data, ID : {nameId}. All StatAcquire result set Default.");
            return;
        }

        if(!temp.LevelDataMap.TryGetValue(level, out var data))
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the Stat Enchant data, Level : {level}. All StatAcquire result set Default.");
            return;
        }
        
        GetStatEnhance(data, out PlayerStatus? status, out CalFormula? formula, out float amount);
        if (status == null || formula == null) return;
        _playerModel.StatusEnhance(status.Value, formula.Value, amount, false);
        
        _enableStatList[nameId] = level;
    }

    private void StatLevelUp(int nameId, int level)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[EnchantCalculator] Not Initialized. All StatAcquire result set Default.");
            return;
        }

        var temp = _repo.GetStatChainByName(EnchantModel.GROUP_MODEL_STAT, nameId);
        if (temp == null)
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the Stat Enchant data, ID : {nameId}. All StatLevelUp result set Default.");
            return;
        }
        
        if (!temp.LevelDataMap.ContainsKey(level))
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the Stat Enchant data, Level : {level}. All StatLevelUp result set Default.");
            return;
        }
        
        if (!_enableStatList.TryGetValue(nameId, out var preLevel))
        {
            StatAcquire(nameId, level);
            return;
        }

        // 과거 데이터 삭제
        var preData = temp.LevelDataMap[preLevel];
        GetStatEnhance(preData, out PlayerStatus? preStatus, out CalFormula? preFormula, out float preAmount);
        if (preStatus == null || preFormula == null) return;
        _playerModel.StatusEnhance(preStatus.Value, preFormula.Value, preAmount, true);

        // 현재 데이터 적용
        var curData = temp.LevelDataMap[level];
        GetStatEnhance(curData, out PlayerStatus? curStatus, out CalFormula? culFormula, out float curAmount);
        if (curStatus == null || culFormula == null) return;
        _playerModel.StatusEnhance(curStatus.Value, culFormula.Value, curAmount, false);

        _enableStatList[nameId] = level;
    }

    private void StatRemoved(int nameId)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[EnchantCalculator] Not Initialized. All StatAcquire result set Default.");
            return;
        }

        var temp = _repo.GetStatChainByName(EnchantModel.GROUP_MODEL_STAT, nameId);
        if (temp == null || !_enableStatList.TryGetValue(nameId, out var level))
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the Stat Enchant data, ID : {nameId}. All StatRemoved result set Default.");
            return;
        }

        var data = temp.LevelDataMap[level];
        GetStatEnhance(data, out PlayerStatus? status, out CalFormula? formula, out float amount);
        if (status == null || formula == null) return;
        _playerModel.StatusEnhance(status.Value, formula.Value, amount, true);

        _enableStatList.Remove(nameId);
    }
    
    // ---------- 효과 계산 ----------
    /// <summary>
    /// 스킬에 미치는 효과에 대한 계산 (보유 인첸트까지 자동으로 계산 됨)
    /// </summary>
    /// <param name="skillId">계산을 실행할 스킬의 ID</param>
    /// <param name="effect1Spec">스킬에 연동된 이펙트의 효과가 계산이 되어있는 구조체<br/>이펙트가 없으면 null 반환</param>
    /// <param name="effect2Spec">스킬에 연동된 이펙트의 효과가 계산이 되어있는 구조체<br/>이펙트가 없으면 null 반환</param>
    public void EffectCalculate(int skillId, out EffectSpec? effect1Spec, out EffectSpec? effect2Spec)
    {
        if (_repo == null)
        {
            _repo = DataManager.Instance.SpellRepo;
        }

        effect1Spec = null;
        effect2Spec = null;
        
        var skillData = _repo.GetSkillData(skillId);
        if (skillData == null)
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the skill data : {skillId}. All Data set null.");
            return;
        }

        EffectTableData effect1Data = null;
        EffectTableData effect2Data = null;
        
        if (skillData.Effect_ID_1 != 0) effect1Data = _repo.GetEffectData(skillData.Effect_ID_1);
        if (skillData.Effect_ID_2 != 0) effect2Data = _repo.GetEffectData(skillData.Effect_ID_2);

        if (effect1Data != null)
        {
            GetEffectEnhance(skillData, effect1Data, out var temp);
            effect1Spec = temp;
        }
        else if (effect2Data != null)
        {
            GetEffectEnhance(skillData, effect2Data, out var temp);
            effect2Spec = temp;
        }
    }
    
    // ---------- 이벤트 핸들러 ----------
    private void HandleStatAcquired(int nameId, int level)
    {
        StatAcquire(nameId, level);
    }

    private void HandleStatLevelUp(int nameId, int level)
    {
        StatLevelUp(nameId, level);
    }

    private void HandleStatRemoved(int nameId)
    {
        StatRemoved(nameId);
    }
    
    // ---------- 보조 함수 ----------
    private bool TryGetStatVariation(StatTableData data, string targetType, out float resultValue)
    {
        if (string.Equals(data.ValueType_1, targetType, StringComparison.Ordinal))
        {
            resultValue = data.Variation_1;
            return true;
        }
    
        if (string.Equals(data.ValueType_2, targetType, StringComparison.Ordinal))
        {
            resultValue = data.Variation_2;
            return true;
        }
    
        if (string.Equals(data.ValueType_3, targetType, StringComparison.Ordinal))
        {
            resultValue = data.Variation_3;
            return true;
        }

        // 일치하는 타입이 없을 경우
        resultValue = 0f;
        return false;
    }
    
    private bool TryGetSkillEffectValue(SkillTableData data, int effectId, string targetType, out float resultValue)
    {
        if (string.Equals(data.E_ValueType_1, targetType, StringComparison.Ordinal) && data.Effect_ID_1 != effectId)
        {
            resultValue = data.E_Variation_1;
            return true;
        }
    
        if (string.Equals(data.E_ValueType_2, targetType, StringComparison.Ordinal) && data.Effect_ID_2 != effectId)
        {
            resultValue = data.E_Variation_2;
            return true;
        }

        // 일치하는 타입이 없을 경우
        resultValue = 0f;
        return false;
    }
    
    private void GetGroupDamageModifier(SkillTableData skillData, 
        out float groupDmgBonus, out float groupCritRateBonus, out float groupCritDmgBonus)
    {
        float dmgResult = 0;
        float critResult = 0;
        float critDmgResult = 0;
        
        foreach (var ownedStat in _enchantModel.OwnedStats.Values)
        {
            var data = ownedStat.Data;
            if (data.Target_2 != skillData.SkillGroup_ID && 
                data.Target_2 != skillData.Tag_ID_1 && data.Target_2 != skillData.Tag_ID_2 && 
                data.Target_2 != skillData.Tag_ID_3 && data.Target_2 != skillData.Tag_ID_4) 
                continue;

            if(TryGetStatVariation(data, DAMAGE, out var temp1)) dmgResult += temp1;
            if(TryGetStatVariation(data, CRITCAL_RATE, out var temp2)) critResult += temp2;
            if(TryGetStatVariation(data, CRITCAL_DAMAGE, out var temp3)) critDmgResult += temp3;
        }
        
        groupDmgBonus = 1 + (dmgResult / 100f);
        groupCritRateBonus = critResult;
        groupCritDmgBonus = critDmgResult;
    }

    private void GetProjectileAdd(SkillTableData skillData, 
        out int addProjectile, out float projectileGap, out float projectileDmgReduce)
    {
        foreach (var ownedStat in _enchantModel.OwnedStats.Values)
        {
            var data = ownedStat.Data;
            if (data.Target_2 == skillData.SkillGroup_ID && data.Target_1 == PROJECTILE_TAG)
            {
                if (skillData.Tag_ID_1 == data.Target_1 || skillData.Tag_ID_2 == data.Target_1 || 
                    skillData.Tag_ID_3 == data.Target_1 || skillData.Tag_ID_4 == data.Target_1)
                {
                    

                    TryGetStatVariation(data, ADD_PROJECTILE, out var temp);
                    TryGetStatVariation(data, PROJECTILE_GAP, out projectileGap);
                    TryGetStatVariation(data, PROJECTILE_DAMAGE_REDUCE, out projectileDmgReduce);

                    addProjectile = Mathf.RoundToInt(temp);
                    
                    return;
                }
            }
        }
        addProjectile = 0;
        projectileGap = 0;
        projectileDmgReduce = 0;
    }

    private void GetSkillAreaExtension(SkillTableData skillData, out float x, out float y)
    {
        x = 1f;
        y = 1f;
        
        foreach (var ownedStat in _enchantModel.OwnedStats.Values)
        {
            var data = ownedStat.Data;
            if (data.Target_2 != skillData.SkillGroup_ID && 
                data.Target_2 != skillData.Tag_ID_1 && data.Target_2 != skillData.Tag_ID_2 && 
                data.Target_2 != skillData.Tag_ID_3 && data.Target_2 != skillData.Tag_ID_4) 
                continue;
            
            
            
            // DB에 표기된 수치는 2.4(%)이므로, 0.024가 아니어서 100으로 추가로 나눔
            if(TryGetStatVariation(data, X_LENGTH_EXTENSION_RATE, out var tempX)) x += (tempX / 100f);
            if(TryGetStatVariation(data, Y_LENGTH_EXTENSION_RATE, out var tempY)) y += (tempY / 100f);
        }
    }

    private void GetEffectEnhance(SkillTableData skillData, EffectTableData effectData, out EffectSpec specData)
    {
        float enchantValue = effectData.Value;
        float enchantInterval = effectData.Interval;
        float enchantDuration = effectData.Duration;
        float effectPower = _playerModel.EffectPower;
        
        foreach (var ownedStat in _enchantModel.OwnedStats.Values)
        {
            var data = ownedStat.Data;
            
            if (data.Target_2 != skillData.SkillGroup_ID) continue;
            if (data.Target_1 != effectData.Effect_ID) continue;
            
            if(TryGetStatVariation(data, VALUE, out var temp)) enchantValue += temp;
            if(TryGetStatVariation(data, INTERVAL, out var temp2)) enchantInterval += temp2;
            if(TryGetStatVariation(data, DURATION, out var temp3)) enchantDuration += temp3;
            
            if(TryGetSkillEffectValue(skillData, effectData.Effect_ID, VALUE, out var temp4)) enchantValue += temp4;
            if(TryGetSkillEffectValue(skillData, effectData.Effect_ID, INTERVAL, out var temp5)) enchantInterval += temp5;
            if(TryGetSkillEffectValue(skillData, effectData.Effect_ID, DURATION, out var temp6)) enchantDuration += temp6;
        }
        
        if (!string.Equals(effectData.EffectType, KNOCK_BACK, StringComparison.Ordinal) && 
            !string.Equals(effectData.EffectType, STATUS_UP, StringComparison.Ordinal))
        {
            enchantDuration = enchantDuration + (effectPower * 0.01f);
        }
        
        specData = new EffectSpec(effectData.Effect_ID, enchantValue, enchantDuration, enchantInterval);
    }

    private void GetStatEnhance(StatTableData data, out PlayerStatus? status, out CalFormula? formula, out float amount)
    {
        if (TryGetStatVariation(data, HP, out var tempHp))
        {
            status = PlayerStatus.Hp;
            formula = CalFormula.Rate;
            amount = tempHp;
        }

        if (TryGetStatVariation(data, ATTACK, out var tempAttack))
        {
            status = PlayerStatus.Attack;
            formula = CalFormula.Rate;
            amount = tempAttack;
        }

        if (TryGetStatVariation(data, ATTACK_SPEED, out var tempAttackSpeed))
        {
            status = PlayerStatus.AttackSpeed;
            formula = CalFormula.Rate;
            amount = tempAttackSpeed;
        }
        
        Debug.LogWarning($"[EnchantCalculator] The {data.ValueType_2} increase type hasn't been set. Please define a new type.");
        status = null;
        formula = null;
        amount = 0;
    }
}
