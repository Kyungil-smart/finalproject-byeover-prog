// 담당자 : 정승우
// 설명   : 스킬 인챈트(LinkedSkillID>0) 획득/강화 시 실제 스킬을 해금·교체한다.
//          스탯 인챈트는 EnchantApplicationSystem이, 스킬 인챈트는 이 시스템이 담당.
//          인챈트 테이블 v1.03 규칙: 자동 공격(60010)은 '일반 스킬 인챈트 획득 시 자동 획득'.

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnchantModel의 획득/레벨업 이벤트를 구독해, 스킬 인챈트면 해당 스킬을 등록한다.
/// 등록은 전부 교체식(같은 스킬군 덮어쓰기)이라 레벨업 시 자연스럽게 상위 레벨로 바뀐다.
/// InGameBootstrap이 생성·주입·구독을 담당.
/// </summary>
public class SkillEnchantSystem : MonoBehaviour
{
    private EnchantModel _enchantModel;
    private SkillSystem _skillSystem;
    private CombinationModel _combinationModel;
    private CombatSystem _combatSystem;

    // 파이어브레스 트리거 주기 (인챈트 테이블 v1.03: Lv1=15 / Lv2=13 / Lv3=10회 자동공격마다)
    private static readonly int[] FireBreathCadence = { 15, 13, 10 };

    public void Initialize(EnchantModel enchantModel, SkillSystem skillSystem,
        CombinationModel combinationModel, CombatSystem combatSystem)
    {
        Unsubscribe();
        _enchantModel = enchantModel;
        _skillSystem = skillSystem;
        _combinationModel = combinationModel;
        _combatSystem = combatSystem;
        Subscribe();
    }

    private void Subscribe()
    {
        if (_enchantModel == null) return;
        _enchantModel.OnSkillAcquired += HandleChanged;
        _enchantModel.OnSkillAcquired += HandleChanged;
    }

    private void Unsubscribe()
    {
        if (_enchantModel == null) return;
        _enchantModel.OnSkillAcquired -= HandleChanged;
        _enchantModel.OnSkillLevelUp -= HandleChanged;
    }

    private void OnDestroy() => Unsubscribe();

    private void HandleChanged(int enchantId, int level)
    {
        Apply(enchantId, level);
    }

    /// <summary>
    /// 이어하기: 세이브된 인챈트의 최종 레벨을 1회씩 적용한다.
    /// (스탯과 달리 스킬 등록은 교체식이라 델타 재생이 필요 없다.)
    /// </summary>
    public void ReapplyFromSave(List<AcquiredEnchantSaveData> saved)
    {
        if (saved == null) return;
        for (int i = 0; i < saved.Count; i++)
            Apply(saved[i].EnchantId, saved[i].Level);
    }

    private void Apply(int enchantId, int level)
    {
        var repo = Legacy_DataManager.Instance != null ? Legacy_DataManager.Instance.CharacterRepo : null;
        var master = repo != null ? repo.GetEnchantMaster(enchantId) : null;
        if (master == null || master.LinkedSkillID <= 0) return; // 스탯 인챈트는 EnchantApplicationSystem 담당

        var spellRepo = DataManager.Instance != null ? DataManager.Instance.SpellRepo : null;
        if (spellRepo == null || _skillSystem == null)
        {
            Debug.LogWarning($"[스킬인챈트] SpellRepo/SkillSystem이 없어 스킬을 등록하지 못했습니다 (enchant {enchantId}).");
            return;
        }

        // 스킬 ID 규칙: 베이스 ID + (레벨-1). 예) 파이어브레스 1011/1012/1013
        int clampedLevel = Mathf.Clamp(level, 1, 3);
        int baseId = master.LinkedSkillID;
        int skillId = baseId + clampedLevel - 1;

        var data = spellRepo.GetSkill(skillId);
        if (data == null)
        {
            Debug.LogWarning($"[스킬인챈트] 스킬 데이터 없음: {skillId} (enchant {enchantId})");
            return;
        }

        switch (baseId)
        {
            case 1011: // 파이어브레스 (일반 스킬 인챈트): 자동공격 N회 트리거
                _skillSystem.ReplaceAutoAttackSkill(FireBreathCadence[clampedLevel - 1], data);
                // 자동 공격(60010)은 일반 스킬 인챈트 획득 시 자동 획득 — 이 시점부터 자동공격 시작.
                if (_combatSystem != null)
                    _combatSystem.EnableAutoAttack();
                break;

            case 1021: // 화염 작렬 (조합): 연두·빨강·파랑 각 1정렬
                if (_combinationModel != null)
                    _combinationModel.SetRecipe(0, new int[] { 2, 0, 1 }, skillId);
                break;

            case 1031: // 화염 정령 소환 (조합): 노랑·파랑·연두 각 1정렬
                if (_combinationModel != null)
                    _combinationModel.SetRecipe(1, new int[] { 3, 1, 2 }, skillId);
                break;

            case 1041: // 대지 균열 (콤보): 콤보 7의 배수
                _skillSystem.ReplaceComboSkill(7, data);
                break;

            case 1051: // 메테오 (콤보): 콤보 9의 배수
                _skillSystem.ReplaceComboSkill(9, data);
                break;

            default:
                Debug.LogWarning($"[스킬인챈트] 미정의 LinkedSkillID={baseId} (enchant {enchantId}) — 등록 로직 추가 필요.");
                return;
        }

        Debug.Log($"[스킬인챈트] '{master.Name}' Lv{clampedLevel} 적용 (skillId={skillId})");
    }
}
