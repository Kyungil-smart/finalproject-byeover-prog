using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnchantCalculator : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private EnchantModel _enchantModel;
    
    // ---------- private ----------
    private SpellRepo _repo;

    // ---------- 이벤트 ----------
    
    
    // ---------- 스킬 계산 ----------
    /// <summary>
    /// 데미지 계산
    /// </summary>
    /// <param name="skillId">계산을 실행할 스킬의 ID</param>
    /// <returns>BaseDamage</returns>
    public int DamageCalculate(int skillId)
    {
        if (_repo == null)
        {
            _repo = DataManager.Instance.SpellRepo;
        }

        var data = _repo.GetSkillData(skillId);
        if (data == null)
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the skill data : {skillId}. All Data set is 0.");
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
        float calCritDmg = 1 + 0.2f * ((100 + (critDmg + groupCritDmgBonus)) / 100f);

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
    /// <param name="supPelletDmgReduce">추가 투사체가 원본 투사체에 비해 데미지가 감소하는 정도 (%)</param>
    /// <returns>이 스킬에 적용해야 될 추가 투사체 개수</returns>
    public int ProjectileAddCalculate(int skillId, out float pelletGap, out float supPelletDmgReduce)
    {
        if (_repo == null)
        {
            _repo = DataManager.Instance.SpellRepo;
        }
        
        var data = _repo.GetSkillData(skillId);
        if (data == null)
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the skill data : {skillId}. All Data set 0.");
            pelletGap = 0;
            supPelletDmgReduce = 0;
            return 0;
        }

        GetProjectileAdd(data, out int addProjectile, out pelletGap, out supPelletDmgReduce);
        
        return addProjectile;
    }

    /// <summary>
    /// 투사체 범위 증가 계산
    /// </summary>
    /// <param name="skillId">계산을 실행할 스킬의 ID</param>
    /// <param name="hitSizeX">기본 히트박스의 X값을 이 값과 곱해야됨</param>
    /// <param name="hitSizeY">기본 히트박스의 Y값을 이 값과 곱해야됨</param>
    public void SkillAreaExtenstionCalculate(int skillId, out float hitSizeX, out float hitSizeY)
    {
        if (_repo == null)
        {
            _repo = DataManager.Instance.SpellRepo;
        }
        
        var data = _repo.GetSkillData(skillId);
        if (data == null)
        {
            Debug.LogWarning($"[EnchantCalculator] Can't find the skill data : {skillId}. All Data set Default.");
            hitSizeX = 1f;
            hitSizeY = 1f;
            return;
        }
        
        GetSkillAreaExtension(data, out hitSizeX, out hitSizeY);
    }
    
    // ---------- 스텟 계산 ----------
    
    
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

            string dmg = "Dmg";
            string crit = "CriticalRate";
            string critDmg = "CriticalDamage";

            if(TryGetStatVariation(data, dmg, out var temp1)) dmgResult += temp1;
            if(TryGetStatVariation(data, crit, out var temp2)) critResult += temp2;
            if(TryGetStatVariation(data, critDmg, out var temp3)) critDmgResult += temp3;
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
            if (data.Target_2 == skillData.SkillGroup_ID && data.Target_1 == 20)
            {
                if (skillData.Tag_ID_1 == data.Target_1 || skillData.Tag_ID_2 == data.Target_1 || 
                    skillData.Tag_ID_3 == data.Target_1 || skillData.Tag_ID_4 == data.Target_1)
                {
                    string activePlusCount = "ActivePlusCount";
                    string pelletGap  = "PelletGap";
                    string supPelletDmg = "SuppPelletDmg";

                    TryGetStatVariation(data, activePlusCount, out var temp);
                    TryGetStatVariation(data, pelletGap, out projectileGap);
                    TryGetStatVariation(data, supPelletDmg, out projectileDmgReduce);

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
            
            string xAreaExtension = "HitSize_X";
            string yAreaExtension = "HitSize_Y";
            
            // DB에 표기된 수치는 2.4(%)이므로, 0.024가 아니어서 100으로 추가로 나눔
            if(TryGetStatVariation(data, xAreaExtension, out var tempX)) x += (tempX / 100f);
            if(TryGetStatVariation(data, yAreaExtension, out var tempY)) y += (tempY / 100f);
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
            
            string value = "Main_Value";
            string interval = "Interval";
            string duration = "Duration";
            
            if(TryGetStatVariation(data, value, out var temp)) enchantValue += temp;
            if(TryGetStatVariation(data, interval, out var temp2)) enchantInterval += temp2;
            if(TryGetStatVariation(data, duration, out var temp3)) enchantDuration += temp3;
            
            if(TryGetSkillEffectValue(skillData, effectData.Effect_ID, value, out var temp4)) enchantValue += temp4;
            if(TryGetSkillEffectValue(skillData, effectData.Effect_ID, interval, out var temp5)) enchantInterval += temp5;
            if(TryGetSkillEffectValue(skillData, effectData.Effect_ID, duration, out var temp6)) enchantDuration += temp6;
        }

        string knockBack = "KnockBack";
        string statusUp = "StatusUP";

        if (effectData.EffectType != knockBack && effectData.EffectType != statusUp)
        {
            enchantDuration = enchantDuration + (effectPower * 0.01f);
        }
        
        specData = new EffectSpec(effectData.Effect_ID, enchantValue, enchantDuration, enchantInterval);
    }
}
