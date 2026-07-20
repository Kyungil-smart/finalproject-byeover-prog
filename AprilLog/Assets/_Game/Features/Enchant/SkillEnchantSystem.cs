// 담당자 : 정승우
// 설명   : 스킬 인챈트(LinkedSkillID>0) 획득/강화 시 실제 스킬을 해금·교체한다.
//          스탯 인챈트는 EnchantApplicationSystem이, 스킬 인챈트는 이 시스템이 담당.
//          인챈트 테이블 v1.03 규칙: 자동 공격(60010)은 '일반 스킬 인챈트 획득 시 자동 획득'.

// 수정자 : 김영찬
// 수정 내용 : 스킬 DB에 기반하여 조합식 등록하는 함수 추가

// 2차 수정자 : 조규민
// 수정 내용 : 인챈트 교체로 제거된 스킬 인챈트가 자동공격/콤보/조합 발동 목록에 남지 않도록 제거 이벤트 처리 추가

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

    // 발동 조건(콤보 배수/자동공격 N회)은 시트 RequiredValue_1이 정본이다.
    // 아래 케이던스 배열과 케이스 안의 숫자들은 시트에 행이 없을 때만 쓰는 폴백으로 남긴다.
    private static int TriggerFromDb(Legacy_SkillData data, int fallback)
    {
        if (data == null) return fallback;
        var repo = DataManager.Instance != null ? DataManager.Instance.SpellRepo : null;
        if (repo == null) return fallback;

        // 트리거 값은 분할(본체/폭발) 스킬도 '본체' 행에 있으므로 데미지용 오버라이드 없이 insert-0 매핑을 쓴다.
        int newId = (data.SkillID / 1000) * 10000 + (data.SkillID % 1000);
        var row = repo.GetSkillData(newId);
        return row != null && row.RequiredValue_1 > 0f ? Mathf.RoundToInt(row.RequiredValue_1) : fallback;
    }

    // 파이어브레스 트리거 주기 (인챈트 테이블 v1.03: Lv1=15 / Lv2=13 / Lv3=10회 자동공격마다)
    private static readonly int[] FireBreathCadence = { 15, 13, 10 };
    private static readonly int[] HasteCadence = { 30, 25, 20 };        // 헤이스트 (바람 일반) — v1.04 테이블
    private static readonly int[] OrbLightningCadence = { 30, 25, 20 }; // 구형 번개 (번개 일반) — v1.04 테이블
    private static readonly int[] WaterBombCadence = { 25, 20, 15 };    // 물 폭탄 (물 일반) — v1.04 테이블(20011~13 자동공격 25/20/15회)
    private static readonly int[] MarchingIceCadence = { 30, 25, 20 };  // 마칭 아이스 (얼음 일반) — v1.04 테이블(50011~13 자동공격 30/25/20회)

    // 인챈트 선택은 새 SkillEnchantTable(카드 Name_ID)을 쓰는데, 이 시스템은 구 Legacy_EnchantMasterTable
    // (EnchantID 101~/301~/401~)을 역조회한다. 연결은 ResolveLegacyEnchantId()가 보유 스킬의 Skill_ID에서 산술 변환하는 걸 1순위로 한다.
    // 아래 표는 보유 데이터가 없을 때(세이브 복구 등)만 쓰는 2순위 폴백. (공식 v1.04 Name ↔ Legacy EnchantMaster 대조)
    // ※ 테이블 v1.04(4)에서 번개 Name 재배정: 방전82 / 벼락83 / 뇌격84 (이전 방전·벼락 Name 82 중복 해소).
    //   SkillEnchantTable.asset도 벼락=83·뇌격=84로 갱신됨 → 폴백표도 이에 맞춤. (1순위 Skill_ID 변환은 항상 정확)
    private static readonly Dictionary<int, int> NameIdToLegacyEnchantId = new Dictionary<int, int>
    {
        { 50, 101 }, { 51, 102 }, { 52, 103 }, { 53, 104 }, { 54, 105 }, // 불: 파이어브레스/화염작렬/화염정령/대지균열/메테오
        { 60, 201 }, { 61, 202 }, { 62, 203 }, { 63, 204 }, { 64, 205 }, // 물: 물폭탄/탄환세례/급류/파도소환/하이드로펌프
        { 70, 301 }, { 71, 302 }, { 72, 303 }, { 73, 304 }, { 74, 305 }, // 바람: 헤이스트/바람칼날/돌풍/허리케인/템페스트
        { 80, 401 }, { 81, 402 }, { 82, 403 }, { 83, 404 }, { 84, 405 }, // 번개: 구형/사슬/방전/벼락/뇌격
        { 90, 501 }, { 91, 502 }, { 92, 503 }, { 93, 504 }, { 94, 505 }, // 얼음: 마칭/글레이셜/빙결/얼음결정/절대영도
        // 물폭탄(Name 60)은 신규 테이블에 투사체(20011)+폭발(20111) 두 엔트리가 같은 Name이라, OwnedSkills가 폭발(20111→idx 11)을 잡으면
        //   1순위 산술변환(idx 1~9 가정)이 실패한다. 이 폴백표가 60→201로 받아줘야 자동공격 등록이 누락되지 않는다.
    };

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
        _enchantModel.OnSkillRemoved += HandleRemoved;
    }

    private void Unsubscribe()
    {
        if (_enchantModel == null) return;
        _enchantModel.OnSkillAcquired -= HandleChanged;
        _enchantModel.OnSkillLevelUp -= HandleChanged;
        _enchantModel.OnSkillRemoved -= HandleRemoved;
    }

    private void OnDestroy() => Unsubscribe();

    private void HandleChanged(int enchantId, int level)
    {
        Apply(enchantId, level);
    }

    private void HandleRemoved(int enchantId)
    {
        Remove(enchantId);
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

    // 카드 Name_ID → 우리 Legacy EnchantID(101~105/301~305/401~405) 변환.
    // 1순위: 보유 스킬의 고유 Skill_ID(5자리)에서 산술 변환 → Name 충돌(공식 v1.04 방전·벼락 둘 다 82)에도 정확, asset 재export에도 안 깨짐.
    //   base=Skill_ID/10(레벨 자리 제거), element=base/1000, idx=base%1000, Legacy=element*100+idx.
    //   예) 40041/10=4004→404(벼락), 40031/10=4003→403(방전), 10011/10=1001→101(파브), 30021/10=3002→302(바람칼날).
    //   물(2xxxx)→201~, 얼음(5xxxx)→501~ 도 Legacy EnchantMaster에 추가돼 정상 등록됨. (물폭탄은 폭발 엔트리 idx>9로 1순위가 실패할 수 있어 폴백표가 60→201로 보정)
    // 2순위: 보유 데이터가 없을 때(세이브 복구 등) Name→Legacy 고정 매핑표 폴백.
    private int ResolveLegacyEnchantId(int nameId)
    {
        if (_enchantModel != null && _enchantModel.OwnedSkills.TryGetValue(nameId, out var owned) && owned.Data != null)
        {
            int baseId = owned.Data.Skill_ID / 10;   // 레벨 자리 제거. 예) 20111→2011, 10011→1001
            int element = baseId / 1000;             // 원소: 1불 2물 3바람 4번개 5얼음
            int idx = baseId % 1000;                 // 스킬 인덱스. 물폭탄 폭발행은 11(=폭발 sub-id), 투사체행은 1
            // 물폭탄처럼 같은 Name에 투사체(2001x)+폭발(2011x) 두 엔트리가 있어 폭발행(idx 11)이 체인을 점령하면
            // idx의 폭발 자리(10단위)를 떼어 원래 스킬 인덱스(1~9)로 정규화한다. 11→1, 12→2 ... → 폴백표 없이도 201로 해석됨.
            if (idx > 9) idx %= 10;
            if (element >= 1 && element <= 9 && idx >= 1 && idx <= 9)
                return element * 100 + idx;
        }
        if (NameIdToLegacyEnchantId.TryGetValue(nameId, out int mapped))
            return mapped;
        return nameId; // 이미 Legacy ID이거나 미지정 — 아래 GetEnchantMaster에서 걸러짐
    }

    private void Apply(int enchantId, int level)
    {
        // enchantId == Name_ID(카드 'Name'). Skill_ID 기준으로 우리 Legacy EnchantID를 구한다.
        int originalName = enchantId;
        int legacyEnchantId = ResolveLegacyEnchantId(enchantId);

        var repo = Legacy_DataManager.Instance != null ? Legacy_DataManager.Instance.CharacterRepo : null;
        var master = repo != null ? repo.GetEnchantMaster(legacyEnchantId) : null;
        if (master == null)
        {
            Debug.Log($"[스킬인챈트] Name {originalName}(→Legacy {legacyEnchantId}) 대응 EnchantMaster 없음 — 물·얼음 등 미구현 원소이거나 미배선. 등록 스킵(불/바람/번개만 구현).");
            return;
        }
        if (master.LinkedSkillID <= 0) return; // 스탯 인챈트는 EnchantApplicationSystem 담당

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
        
        // DB 데이터로 조합 스킬 여부 검증 및 동적 할당
        // enchantId가 DB의 Name_ID이므로, 조합 스킬 그룹(EnchantModel.GROUP_COMBINATION_SKILL)에 이 스킬이 있는지 검색
        var combinationChain = spellRepo.GetSkillChainByName(EnchantModel.GROUP_COMBINATION_SKILL, enchantId);
        if (combinationChain != null)
        {
            var newSkillData = combinationChain.GetNextLevelData(clampedLevel - 1);
            if (newSkillData != null)
            {
                // 신규 테이블의 데이터를 읽어와서 동적으로 조합 슬롯 등록 (키=nameId=enchantId, FusionData와 일치)
                RegisterCombinationFromTable(newSkillData, enchantId, skillId);
                Debug.Log($"[스킬인챈트] '{master.Name}' Lv{clampedLevel} 조합식 동적 적용 (skillId: {skillId})");
                
                return;
            }
        }
        
        // DB에 없는 값을 삽입 시 레거시 데이터 활용
        var data = spellRepo.GetSkill(skillId);
        if (data == null)
        {
            Debug.LogWarning($"[스킬인챈트] 스킬 데이터 없음: {skillId} (enchant {enchantId})");
            return;
        }

        switch (baseId)
        {
            case 1011: // 파이어브레스 (일반 스킬 인챈트): 자동공격 N회 트리거
                _skillSystem.ReplaceAutoAttackSkill(TriggerFromDb(data, FireBreathCadence[clampedLevel - 1]), data);
                // 자동 공격(60010)은 일반 스킬 인챈트 획득 시 자동 획득 — 이 시점부터 자동공격 시작.
                if (_combatSystem != null)
                    _combatSystem.EnableAutoAttack();
                break;

            case 1021: // 화염 작렬 (조합): 연두·빨강·파랑 각 1정렬
                _combinationModel?.RegisterRecipe(baseId, new int[] { 2, 0, 1 }, skillId);
                break;

            case 1031: // 화염 정령 소환 (조합): 노랑·파랑·연두 각 1정렬
                _combinationModel?.RegisterRecipe(baseId, new int[] { 3, 1, 2 }, skillId);
                break;

            case 1041: // 대지 균열 (콤보): 콤보 7의 배수
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 7), data);
                break;

            case 1051: // 메테오 (콤보): 콤보 9의 배수
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 9), data);
                break;

            // ===== 바람 속성 (placeholder — VFX·버프·관통·CC 미구현, 데미지/발동만) =====
            case 3011: // 헤이스트 (일반): 자동공격 N회 발동. 발동 시 CombatSystem이 공격력↑+자동공격 간격↓ 버프 활성(StandardID 301 감지). 보조 투사체=PelletCount.
                _skillSystem.ReplaceAutoAttackSkill(TriggerFromDb(data, HasteCadence[clampedLevel - 1]), data);
                if (_combatSystem != null) _combatSystem.EnableAutoAttack();
                break;
            // 조합 인챈트는 RegisterRecipe로 '인챈트 선택 순서대로 가장 왼쪽 빈 슬롯'에 배치된다 (기획 3-2-1, 최대 3개).
            case 3021: // 바람 칼날 (조합): 빨강·노랑·하양 (v1.04) — 느린 관통 투사체(관통 15/20/25, 이즈궁식). 발사/관통은 SkillSystem.FireOneProjectile.
                _combinationModel?.RegisterRecipe(baseId, new int[] { 0, 3, 4 }, skillId);
                break;
            case 3031: // 돌풍 (조합): 파랑·초록·하양 (v1.04) — 3타 후 마지막 펄스=폭발+넉백(위로), Lv3 범위↑. CC는 SkillSystem.DealHazardDamage.
                _combinationModel?.RegisterRecipe(baseId, new int[] { 1, 2, 4 }, skillId);
                break;
            case 3041: // 허리케인 (콤보): 콤보 10의 배수 (v1.04) — 지속 장판 + 매 타격 슬로우(50%). 슬로우는 SkillSystem.DealHazardDamage→MonsterAI.ApplySlow.
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 10), data);
                break;
            case 3051: // 템페스트 (콤보): 콤보 10의 배수 (v1.04) — 랜덤 타겟 + 관통(8/10/12) + 피격당 8히트(NumberOfCycle). SkillSystem.FireOneProjectile.
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 10), data);
                break;

            // ===== 번개 속성 (placeholder — VFX·체인·CC 미구현, 데미지/발동만) =====
            case 4011: // 구형 번개 (일반): 자동공격 N회 — 지속 장판(12틱)
                _skillSystem.ReplaceAutoAttackSkill(TriggerFromDb(data, OrbLightningCadence[clampedLevel - 1]), data);
                if (_combatSystem != null) _combatSystem.EnableAutoAttack();
                break;
            case 4021: // 에너지 볼 (조합): 적들 사이를 튕겨다니는 전기 구 (Lv1·2=3회/Lv3=4회 탐색). EnergyBallRoutine.
                _combinationModel?.RegisterRecipe(baseId, new int[] { 3, 2, 0 }, skillId);
                break;
            case 4031: // 방전 (조합): 파랑·빨강·하양 (v1.04) — 지속 장판 + 첫 피격 몬스터 슬로우(Lv3 지속↑). LightningDischargeRoutine.
                _combinationModel?.RegisterRecipe(baseId, new int[] { 1, 0, 4 }, skillId);
                break;
            case 4041: // 벼락 (콤보): 콤보 9의 배수 (v1.04) — 엘리트/보스 우선 타겟 + 정사각 4히트 + Lv3 스턴(1.5초). DealHazardDamage/HazardRoutine.
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 9), data);
                break;
            case 4051: // 뇌격 (콤보): 콤보 10의 배수 (v1.04) — 세로 직사각 장판
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 10), data);
                break;

            // ===== 물 속성 (골격 — placeholder VFX=색 사각형, 상태이상(슬로우)·이동장판은 폴리싱) =====
            case 2011: // 물 폭탄 (일반): 자동공격 N회 — 착탄 장판(파이어브레스식). 슬로우(13002) 미구현.
                _skillSystem.ReplaceAutoAttackSkill(TriggerFromDb(data, WaterBombCadence[clampedLevel - 1]), data);
                if (_combatSystem != null) _combatSystem.EnableAutoAttack();
                break;
            // 조합 인챈트는 RegisterRecipe로 '인챈트 선택 순서대로 빈 슬롯'에 배치 (기획 3-2-1, 최대 3개).
            case 2021: // 탄환 세례 (조합): 하양·초록·노랑 (v1.04) — 도트·슬로우 미구현, 현재 장판 데미지만
                _combinationModel?.RegisterRecipe(baseId, new int[] { 4, 2, 3 }, skillId);
                break;
            case 2031: // 급류 (조합): 하양·노랑·파랑 (v1.04) — 전체 폭 띠 장판 4히트
                _combinationModel?.RegisterRecipe(baseId, new int[] { 4, 3, 1 }, skillId);
                break;
            case 2041: // 파도 소환 (콤보): 콤보 8의 배수 (v1.04) — 이동장판 미구현, 현재 전방 단발 장판
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 8), data);
                break;
            case 2051: // 하이드로 펌프 (콤보): 콤보 10의 배수 (v1.04) — 세로 직사각 장판
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 10), data);
                break;

            // ===== 얼음 속성 (골격 — placeholder VFX=색 사각형, 빙결/슬로우 CC·이동장판은 폴리싱) =====
            case 5011: // 마칭 아이스 (일반): 자동공격 N회 — 전방 전진 장판 6/7/8펄스
                _skillSystem.ReplaceAutoAttackSkill(TriggerFromDb(data, MarchingIceCadence[clampedLevel - 1]), data);
                if (_combatSystem != null) _combatSystem.EnableAutoAttack();
                break;
            case 5021: // 글레이셜 피어스 (조합): 빨강·노랑·파랑 (v1.04) — 관통 미구현, 현재 단발 투사체
                _combinationModel?.RegisterRecipe(baseId, new int[] { 0, 3, 1 }, skillId);
                break;
            case 5031: // 빙결 지대 (조합): 초록·하양·빨강 (v1.04) — 빙결 CC 미구현, 현재 장판 데미지만
                _combinationModel?.RegisterRecipe(baseId, new int[] { 2, 4, 0 }, skillId);
                break;
            case 5041: // 얼음 결정 (콤보): 콤보 5의 배수 (기획서 3-1) — IceStorm VFX + 슬로우(Lv3 지속↑). 이동장판(player→target)은 폴리싱.
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 5), data);
                break;
            case 5051: // 절대영도 (콤보): 콤보 5의 배수 (기획서 3-2) — 최단거리 사각 장판(VFX=빙결 재사용). 전용 에셋/2초 빙벽 연출은 폴리싱.
                _skillSystem.ReplaceComboSkill(TriggerFromDb(data, 5), data);
                break;

            default:
                Debug.LogWarning($"[스킬인챈트] 미정의 LinkedSkillID={baseId} (enchant {enchantId}) — 등록 로직 추가 필요.");
                return;
        }

        Debug.Log($"[스킬인챈트] '{master.Name}' Lv{clampedLevel} 적용 (skillId={skillId})");
    }
    
    // DB기반 스킬 획득 처리 시 사용
    private void Remove(int enchantId)
    {
        int legacyEnchantId = ResolveLegacyEnchantId(enchantId);

        var repo = Legacy_DataManager.Instance != null ? Legacy_DataManager.Instance.CharacterRepo : null;
        var master = repo != null ? repo.GetEnchantMaster(legacyEnchantId) : null;
        if (master == null || master.LinkedSkillID <= 0)
        {
            return;
        }

        int baseId = master.LinkedSkillID;
        int standardId = baseId / 10;

        _skillSystem?.UnregisterAutoAttackSkillByStandardId(standardId);
        _skillSystem?.UnregisterComboSkillByStandardId(standardId);

        _combinationModel?.UnregisterRecipe(enchantId);
        _combinationModel?.UnregisterRecipe(baseId);

        Debug.Log($"[SkillEnchantSystem] Removed enchant trigger. nameId={enchantId}, legacyId={legacyEnchantId}, standardId={standardId}");
    }

    private void RegisterCombinationFromTable(SkillTableData skillData, int nameId, int currentSkillId)
    {
        if (skillData.SkillGroup_ID == EnchantModel.GROUP_COMBINATION_SKILL)
        {
            List<int> ingredients = new List<int>();

            int req1 = ConvertRawIdToUnitType(skillData.RequiredValue_1);
            int req2 = ConvertRawIdToUnitType(skillData.RequiredValue_2);
            int req3 = ConvertRawIdToUnitType(skillData.RequiredValue_3);

            if (req1 != (int)UnitType.None) ingredients.Add(req1);
            if (req2 != (int)UnitType.None) ingredients.Add(req2);
            if (req3 != (int)UnitType.None) ingredients.Add(req3);

            // 조합 슬롯 등록 키 = nameId(카드 Name). EnchantCombinationModel.FusionData가 nameId 키라
            // View.SetRecipe의 아이콘 조회(FusionData[recipeKey])와 키를 일치시켜야 박스가 채워진다.
            _combinationModel.RegisterRecipe(nameId, ingredients.ToArray(), currentSkillId);
        }
    }
    
    // FusionEnchantData 변환
    private int ConvertRawIdToUnitType(float rawValue)
    {
        int id = Mathf.RoundToInt(rawValue);
        return id switch
        {
            1001 => (int)UnitType.Red,
            1002 => (int)UnitType.Blue,
            1003 => (int)UnitType.Yellow, // DB 1003 -> Enum 3 (Yellow)
            1004 => (int)UnitType.Green,  // DB 1004 -> Enum 2 (Green)
            1005 => (int)UnitType.Purple, // 하양/보라
            _ => (int)UnitType.None
        };
    }
}
