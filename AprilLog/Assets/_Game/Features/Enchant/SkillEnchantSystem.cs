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
    private static readonly int[] HasteCadence = { 30, 25, 20 };        // 헤이스트 (바람 일반) — v1.04 테이블
    private static readonly int[] OrbLightningCadence = { 30, 25, 20 }; // 구형 번개 (번개 일반) — v1.04 테이블

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
        _enchantModel.OnSkillLevelUp += HandleChanged;
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

            // ===== 바람 속성 (placeholder — VFX·버프·관통·CC 미구현, 데미지/발동만) =====
            case 3011: // 헤이스트 (일반): 자동공격 N회 — 버프 미구현, 현재 보조 투사체만 발사
                _skillSystem.ReplaceAutoAttackSkill(HasteCadence[clampedLevel - 1], data);
                if (_combatSystem != null) _combatSystem.EnableAutoAttack();
                break;
            // ⚠ 조합 슬롯은 MAX_RECIPES=3개뿐이고 불이 slot 0,1을 점유 → 바람/번개 조합은 빈 slot 2를 공유한다.
            //    즉 바람칼날/돌풍/사슬번개/방전 중 '마지막에 획득한 1종'만 활성. 동시 활성하려면 MAX_RECIPES+UI 슬롯 확장 필요.
            case 3021: // 바람 칼날 (조합): 빨강·노랑·하양 (v1.04) — 관통 미구현, 현재 단발 투사체
                _combinationModel?.SetRecipe(2, new int[] { 0, 3, 4 }, skillId);
                break;
            case 3031: // 돌풍 (조합): 파랑·초록·하양 (v1.04) — 넉백 미구현, 현재 장판 데미지만
                _combinationModel?.SetRecipe(2, new int[] { 1, 2, 4 }, skillId);
                break;
            case 3041: // 허리케인 (콤보): 콤보 10의 배수 (v1.04) — 슬로우 미구현, 현재 지속 장판 데미지만
                _skillSystem.ReplaceComboSkill(10, data);
                break;
            case 3051: // 템페스트 (콤보): 콤보 10의 배수 (v1.04) — 관통·8히트 미구현, 현재 단발 투사체
                _skillSystem.ReplaceComboSkill(10, data);
                break;

            // ===== 번개 속성 (placeholder — VFX·체인·CC 미구현, 데미지/발동만) =====
            case 4011: // 구형 번개 (일반): 자동공격 N회 — 지속 장판(12틱)
                _skillSystem.ReplaceAutoAttackSkill(OrbLightningCadence[clampedLevel - 1], data);
                if (_combatSystem != null) _combatSystem.EnableAutoAttack();
                break;
            case 4021: // 사슬 번개 (조합): 노랑·초록·빨강 (v1.04, slot 2 공유) — 체인 미구현, 현재 단발
                _combinationModel?.SetRecipe(2, new int[] { 3, 2, 0 }, skillId);
                break;
            case 4031: // 방전 (조합): 파랑·빨강·하양 (v1.04, slot 2 공유) — 슬로우 미구현, 현재 지속 장판 데미지만
                _combinationModel?.SetRecipe(2, new int[] { 1, 0, 4 }, skillId);
                break;
            case 4041: // 벼락 (콤보): 콤보 9의 배수 (v1.04) — 스턴·엘리트우선 미구현, 현재 정사각 4히트 장판
                _skillSystem.ReplaceComboSkill(9, data);
                break;
            case 4051: // 뇌격 (콤보): 콤보 10의 배수 (v1.04) — 세로 직사각 장판
                _skillSystem.ReplaceComboSkill(10, data);
                break;

            default:
                Debug.LogWarning($"[스킬인챈트] 미정의 LinkedSkillID={baseId} (enchant {enchantId}) — 등록 로직 추가 필요.");
                return;
        }

        Debug.Log($"[스킬인챈트] '{master.Name}' Lv{clampedLevel} 적용 (skillId={skillId})");
    }
}
