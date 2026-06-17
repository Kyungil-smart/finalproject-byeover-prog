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
        
        // Base_Damage = [ ATK x ( 1 + Stat_ATK_Enchant / 100) x (1 + Skill_Enchant/100) x (1+ Group_Bonus / 100) x CriticalModifier]
        int attack = _playerModel.Attack;
        float critRate = _playerModel.CriticalRate;
        float critDmg = _playerModel.CriticalDamage;
        
        float skillDmgBonus = 1 + (data.Dmg / 100f);
        float skillCritRateBonus = data.CriticalRate;
        
        GetGroupDamageModifier(skillId, data.Tag_ID_1, data.Tag_ID_2, data.Tag_ID_3, data.Tag_ID_4, 
            out float groupDmgBonus, out float groupCritRateBonus, out float groupCritDmgBonus);

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

        GetProjectileAdd(data.SkillGroup_ID, data.Tag_ID_1, data.Tag_ID_2, data.Tag_ID_3, data.Tag_ID_4,
            out int addProjectile, out pelletGap, out supPelletDmgReduce);
        
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
        
        GetSkillAreaExtension(data.SkillGroup_ID, data.Tag_ID_1, data.Tag_ID_2, data.Tag_ID_3, data.Tag_ID_4,
            out hitSizeX, out hitSizeY);
    }
    
    // ---------- 스텟 계산 ----------
    
    
    // ---------- 효과 계산 ----------
    
    
    // ---------- 보조 함수 ----------
    private bool TryGetVariation(StatTableData data, string targetType, out float resultValue)
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
    
    private void GetGroupDamageModifier(int skillGroupID, int tag1, int tag2, int tag3, int tag4, 
        out float groupDmgBonus, out float groupCritRateBonus, out float groupCritDmgBonus)
    {
        float dmgResult = 0;
        float critResult = 0;
        float critDmgResult = 0;
        
        foreach (var ownedStat in _enchantModel.OwnedStats.Values)
        {
            var data = ownedStat.Data;
            if (data.Target_2 != skillGroupID && 
                data.Target_2 != tag1 && data.Target_2 != tag2 && data.Target_2 != tag3 && data.Target_2 != tag4) 
                continue;

            string dmg = "Dmg";
            string crit = "CriticalRate";
            string critDmg = "CriticalDamage";

            if(TryGetVariation(data, dmg, out float temp1)) dmgResult += temp1;
            if(TryGetVariation(data, crit, out float temp2)) critResult += temp2;
            if(TryGetVariation(data, critDmg, out float temp3)) critDmgResult += temp3;
        }
        
        groupDmgBonus = 1 + (dmgResult / 100f);
        groupCritRateBonus = critResult;
        groupCritDmgBonus = critDmgResult;
    }

    private void GetProjectileAdd(int skillGroupID, int tag1, int tag2, int tag3, int tag4,
        out int addProjectile, out float projectileGap, out float projectileDmgReduce)
    {
        foreach (var ownedStat in _enchantModel.OwnedStats.Values)
        {
            var data = ownedStat.Data;
            if (data.Target_2 == skillGroupID && data.Target_1 == 20)
            {
                if (tag1 == data.Target_1 || tag2 == data.Target_1 || tag3 == data.Target_1 || tag4 == data.Target_1)
                {
                    string activePlusCount = "ActivePlusCount";
                    string pelletGap  = "PelletGap";
                    string supPelletDmg = "SuppPelletDmg";

                    TryGetVariation(data, activePlusCount, out float temp);
                    TryGetVariation(data, pelletGap, out projectileGap);
                    TryGetVariation(data, supPelletDmg, out projectileDmgReduce);

                    addProjectile = Mathf.RoundToInt(temp);
                    
                    return;
                }
            }
        }
        addProjectile = 0;
        projectileGap = 0;
        projectileDmgReduce = 0;
    }

    private void GetSkillAreaExtension(int skillGroupID, int tag1, int tag2, int tag3, int tag4,
        out float x, out float y)
    {
        x = 1f;
        y = 1f;
        
        foreach (var ownedStat in _enchantModel.OwnedStats.Values)
        {
            var data = ownedStat.Data;
            if (data.Target_2 != skillGroupID && 
                data.Target_2 != tag1 && data.Target_2 != tag2 && data.Target_2 != tag3 && data.Target_2 != tag4) 
                continue;
            
            string xAreaExtension = "HitSize_X";
            string yAreaExtension = "HitSize_Y";
            
            // DB에 표기된 수치는 2.4(%)이므로, 0.024가 아니어서 100으로 추가로 나눔
            if(TryGetVariation(data, xAreaExtension, out float tempX)) x += (tempX / 100f);
            if(TryGetVariation(data, yAreaExtension, out float tempY)) y += (tempY / 100f);
        }
    }
}
