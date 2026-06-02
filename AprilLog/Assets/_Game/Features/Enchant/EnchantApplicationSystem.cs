// 담당자 : 정승우
// 설명   : 인챈트 획득/레벨업 시 그 효과를 실제로 플레이어/스킬에 적용하는 시스템.
//          EnchantModel의 이벤트를 구독해 PlayerModel 스탯 또는 SkillSystem 스킬에 반영한다.
//
// ※ 인챈트 데이터(LinkedStatType 코드 체계, Add/Rate 여부, 레벨 누적 규칙)가 아직 미확정이라
//   "파이프라인(구독→데이터 조회→분기)"만 완성하고, 구체 매핑은 확장 지점(ToDo)으로 열어둔다.
//   매핑이 비어 있는 동안에는 잘못된 값을 적용하지 않고 경고만 남겨 안전하게 둔다.
//   데이터가 나오면 ResolveStatType()과 ApplyStatEnchant()의 switch만 채우면 동작한다.

using UnityEngine;

/// <summary>
/// 인챈트 효과 적용 시스템. EnchantModel 이벤트 → PlayerModel/SkillSystem 반영.
/// 참조가 비어 있으면 런타임에 자동 탐색하므로 인스펙터 연결은 선택사항이다.
/// </summary>
public class EnchantApplicationSystem : MonoBehaviour
{
    [Header("참조 (비면 런타임 자동 탐색)")]
    [SerializeField] private EnchantModel _enchantModel;
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private SkillSystem _skillSystem;

    private bool _subscribed;

    // ---------- 생명주기 ----------
    private void OnEnable()
    {
        ResolveReferences();

        if (_enchantModel == null)
        {
            Debug.LogWarning("[EnchantApply] EnchantModel을 찾지 못해 인챈트 적용이 비활성화됩니다.", this);
            return;
        }

        if (_subscribed) return;

        _enchantModel.OnEnchantAcquired += ApplyEnchant;
        _enchantModel.OnEnchantLevelUp += ApplyEnchant;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (!_subscribed || _enchantModel == null) return;

        _enchantModel.OnEnchantAcquired -= ApplyEnchant;
        _enchantModel.OnEnchantLevelUp -= ApplyEnchant;
        _subscribed = false;
    }

    // ---------- 적용 진입점 ----------
    /// <summary>
    /// 인챈트 1개의 효과를 적용한다. 획득(level=1)/레벨업(level=newLevel) 이벤트가 직접 호출하고,
    /// 이어하기 재적용(ReapplyAll)도 이 메서드를 사용한다.
    /// </summary>
    public void ApplyEnchant(int enchantId, int level)
    {
        var repo = Legacy_DataManager.Instance != null ? Legacy_DataManager.Instance.CharacterRepo : null;
        if (repo == null)
        {
            Debug.LogWarning("[EnchantApply] CharacterRepo가 없어 인챈트 데이터를 조회할 수 없습니다.");
            return;
        }

        var master = repo.GetEnchantMaster(enchantId);
        if (master == null)
        {
            Debug.LogWarning($"[EnchantApply] 인챈트 마스터 데이터 없음. EnchantID: {enchantId}");
            return;
        }

        var levelData = repo.GetEnchantLevel(enchantId, level);
        float value = levelData != null ? levelData.Value : 0f;

        // 스킬과 연결된 인챈트(LinkedSkillID > 0)는 스킬 인챈트, 그 외는 스탯 인챈트로 취급.
        if (master.LinkedSkillID > 0)
            ApplySkillEnchant(master, level, value);
        else
            ApplyStatEnchant(master, level, value);
    }

    /// <summary>
    /// 보유 중인 모든 인챈트 효과를 일괄 재적용한다. (이어하기로 인챈트가 복원된 직후 호출)
    /// EnchantModel.RestoreFromSave는 이벤트를 발행하지 않으므로 여기서 재적용해야 한다.
    /// ※ ApplyEnchant가 "레벨 L 기준 총량"을 적용하도록 매핑이 구현돼야 정확하다(데이터 확정 후).
    /// </summary>
    public void ReapplyAll()
    {
        ResolveReferences();
        if (_enchantModel == null) return;

        var owned = _enchantModel.ToSaveData();
        if (owned == null) return;

        for (int i = 0; i < owned.Count; i++)
            ApplyEnchant(owned[i].enchantId, owned[i].level);
    }

    // ---------- 스탯 인챈트 적용 ----------
    private void ApplyStatEnchant(Legacy_EnchantMasterData master, int level, float value)
    {
        if (_playerModel == null)
        {
            Debug.LogWarning("[EnchantApply] PlayerModel이 없어 스탯 인챈트를 적용할 수 없습니다.");
            return;
        }

        EnchantStatType stat = ResolveStatType(master.LinkedStatType);

        // ToDo(데이터 확정 후):
        //   1) Add/Rate 구분 — 현재는 플랫 가산(_Add)만 사용. 퍼센트 인챈트면 _Rate 계열로.
        //   2) 레벨 누적 규칙 — 레벨업 시 "증가분(value[L]-value[L-1])"을 더할지 "총량"으로 갱신할지 확정.
        //      (지금은 ResolveStatType이 Unknown을 반환하므로 실제 적용되지 않아 안전)
        switch (stat)
        {
            case EnchantStatType.Attack:
                _playerModel.ApplyAttackBonus_Add(Mathf.RoundToInt(value));
                break;
            case EnchantStatType.MaxHP:
                _playerModel.ApplyHpBonus_Add(Mathf.RoundToInt(value));
                break;
            case EnchantStatType.CritRate:
                _playerModel.ApplyCriRateBonus_Add(Mathf.RoundToInt(value));
                break;
            case EnchantStatType.CritDamage:
                _playerModel.ApplyCriDmgBonus_Add(Mathf.RoundToInt(value));
                break;

            // ToDo: 아래 스탯들은 PlayerModel에 적용 메서드 추가 후 연결
            //       (Penetration / StunPower / SlowPower / CCDuration / SkillDamage / GroupBonus)

            case EnchantStatType.Unknown:
            default:
                Debug.LogWarning($"[EnchantApply] 미정의 스탯 타입(LinkedStatType={master.LinkedStatType}, " +
                                 $"EnchantID={master.EnchantID}). 데이터 확정 후 ResolveStatType 매핑 필요.");
                break;
        }
    }

    // 인챈트 데이터의 LinkedStatType 정수 코드 → 내부 스탯 enum.
    // ※ 코드 체계가 미확정이라 현재는 보류(Unknown). 데이터 확정 시 이 함수만 채우면 된다.
    //    예) 코드값을 enum과 1:1로 맞춘다면:  return (EnchantStatType)linkedStatType;
    private static EnchantStatType ResolveStatType(int linkedStatType)
    {
        // ToDo: 능력치 시트의 LinkedStatType 코드 ↔ 스탯 매핑 확정 후 작성
        return EnchantStatType.Unknown;
    }

    // ---------- 스킬 인챈트 적용 ----------
    private void ApplySkillEnchant(Legacy_EnchantMasterData master, int level, float value)
    {
        if (_skillSystem == null)
        {
            Debug.LogWarning("[EnchantApply] SkillSystem이 없어 스킬 인챈트를 적용할 수 없습니다.");
            return;
        }

        // ToDo(데이터 확정 후): 스킬 인챈트 등록/강화 규칙 연결.
        //   - 정렬 스킬(RegisterSortSkill) / 콤보 스킬(RegisterComboSkill) 구분
        //   - EnchantType(일반/조합형/콤보형), 콤보 배수 등 데이터 반영
        Debug.Log($"[EnchantApply] 스킬 인챈트 적용 보류(데이터 미확정). " +
                  $"EnchantID={master.EnchantID}, SkillID={master.LinkedSkillID}, Lv={level}");
    }

    // ---------- 참조 자동 탐색 ----------
    private void ResolveReferences()
    {
        if (_enchantModel == null) _enchantModel = FindFirstObjectByType<EnchantModel>();
        if (_playerModel == null) _playerModel = FindFirstObjectByType<PlayerModel>();
        if (_skillSystem == null) _skillSystem = FindFirstObjectByType<SkillSystem>();
    }
}

/// <summary>
/// 인챈트가 영향을 주는 플레이어 스탯 종류.
/// 데이터의 LinkedStatType 코드와의 매핑은 EnchantApplicationSystem.ResolveStatType에서 정의한다.
/// </summary>
public enum EnchantStatType
{
    Unknown = 0,
    Attack,
    MaxHP,
    CritRate,
    CritDamage,
    Penetration,
    StunPower,
    SlowPower,
    CCDuration,
    SkillDamage,
    GroupBonus,
}
