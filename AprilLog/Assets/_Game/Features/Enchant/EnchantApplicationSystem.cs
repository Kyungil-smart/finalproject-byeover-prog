// 담당자 : 정승우 (인챈트 효과 적용 시스템 신설)
// 설명   : 레벨업 시 획득/강화한 인챈트의 효과를 PlayerModel에 실제로 반영한다.
//          EnchantModel.OnEnchantAcquired / OnEnchantLevelUp 를 구독하는 유일한 소비자.
//          (이전엔 구독자가 없어 인챈트를 골라도 스탯이 전혀 변하지 않았다.)
//
// ⚠️ INTERIM: LinkedStatType 코드 체계(어떤 스탯을 올리는지)가 기획/데이터에 아직 미확정.
//    매핑(INTERIM): 1=Attack(Rate %), 2=Pierce(Add, 데모 비활성), 3=MaxHP(flat Add),
//    4=CritRate(Add 소수), 5=CritDmg(Add 소수).
//    기획에서 stat-type enum이 확정되면 ApplyStat()의 switch 만 교체하면 된다.
//    또한 데이터에 Add/Rate 구분 필드가 없어, 적용 방식은 코드 컨벤션으로 정한다.

// 수정자 : 김영찬
// 수정 내용 : 저장 관련 클래스 변수명 변경되어 적용함

// 수정자 : 김영찬
// 수정 내용 : 해당 부분이 확정된 인첸트 기획과 맞지 않아 Legacy처리 함

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인챈트 획득/강화 시 그 효과를 PlayerModel에 적용한다.
/// InGameBootstrap이 생성·주입·구독을 담당(없으면 자동 생성).
/// </summary>
public class Legacy_EnchantApplicationSystem : MonoBehaviour
{
    // ⚠️ INTERIM: 현재 더미 데이터의 LinkedStatType 코드 체계. (기획 enum 확정 시 교체)
    private const int StatType_Attack = 1;
    private const int StatType_Pierce = 2;
    private const int StatType_MaxHP = 3;
    private const int StatType_CritRate = 4;
    private const int StatType_CritDmg = 5;

    [SerializeField] private PlayerModel _playerModel;
    private EnchantModel _enchantModel;

    /// <summary>PlayerModel/EnchantModel 주입 후 이벤트 구독. 재호출 시 중복구독 방지.</summary>
    public void Initialize(PlayerModel playerModel, EnchantModel enchantModel)
    {
        Unsubscribe();
        if (playerModel != null) _playerModel = playerModel;
        _enchantModel = enchantModel;
        Subscribe();
    }

    private void Subscribe()
    {
        if (_enchantModel == null) return;
        _enchantModel.OnStatAcquired += HandleAcquired;
        _enchantModel.OnStatLevelUp += HandleLevelUp;
    }

    private void Unsubscribe()
    {
        if (_enchantModel == null) return;
        _enchantModel.OnStatAcquired -= HandleAcquired;
        _enchantModel.OnStatLevelUp -= HandleLevelUp;
    }

    private void OnDestroy() => Unsubscribe();

    // 신규 획득(level 1) / 강화: 직전 레벨 대비 증가분(델타)만 적용한다.
    private void HandleAcquired(int enchantId, int level)
    {
        Debug.Log($"[인챈트이벤트] 신규획득 id={enchantId} lv={level} → 효과 적용 시도");
        ApplyDelta(enchantId, level);
    }

    private void HandleLevelUp(int enchantId, int newLevel)
    {
        Debug.Log($"[인챈트이벤트] 레벨업 id={enchantId} lv={newLevel} → 효과 적용 시도");
        ApplyDelta(enchantId, newLevel);
    }

    /// <summary>
    /// 이어하기: 세이브된 인챈트들을 라이브 강화와 "동일한 순서의 레벨별 델타"로 재적용한다.
    /// (RestoreFromSave는 이벤트를 발행하지 않으므로 별도 재적용이 필요.)
    /// 누적값을 한 번에 적용하면 Rate(곱셈) 스탯에서 라이브 결과(×1.1³)와 누적 1회(×1.3)가
    /// 어긋나 세이브/로드 시 스탯이 변하므로, 반드시 lv 1..level 델타를 순서대로 재생한다.
    /// PlayerModel이 base+아웃게임보너스로 막 초기화된 직후에 호출되어야 한다.
    /// </summary>
    public void ReapplyFromSave(List<AcquiredEnchantSaveData> saved)
    {
        if (saved == null) return;
        for (int i = 0; i < saved.Count; i++)
        {
            int enchantId = saved[i].EnchantId;
            int level = saved[i].Level;
            for (int lv = 1; lv <= level; lv++)
                ApplyDelta(enchantId, lv);
        }
    }

    // 실시간 획득/강화 + 이어하기 재생 공용: Value[level] - Value[level-1] 만큼만 적용.
    // 라이브와 이어하기가 동일 경로를 타므로 세이브/로드 왕복 시 스탯이 보존된다.
    private void ApplyDelta(int enchantId, int level)
    {
        if (!TryGetMaster(enchantId, out var master)) return;

        // 스킬 인챈트(LinkedSkillID>0)는 SkillEnchantSystem이 스킬 등록으로 처리한다. 여기선 스탯만.
        if (master.LinkedSkillID > 0) return;

        float current = GetValue(enchantId, level);
        float prev = level > 1 ? GetValue(enchantId, level - 1) : 0f;
        ApplyStat(enchantId, master.LinkedStatType, current - prev);
    }

    private bool TryGetMaster(int enchantId, out Legacy_EnchantMasterData master)
    {
        master = null;
        if (_playerModel == null)
        {
            Debug.LogWarning("[EnchantApplication] PlayerModel이 없어 인챈트 효과를 적용할 수 없습니다.");
            return false;
        }

        Legacy_CharacterRepo repo = GetRepo();
        if (repo == null)
        {
            Debug.LogWarning("[EnchantApplication] Legacy CharacterRepo를 찾을 수 없습니다.");
            return false;
        }

        master = repo.GetEnchantMaster(enchantId);
        if (master == null)
        {
            Debug.LogWarning($"[EnchantApplication] EnchantMaster 데이터 없음: {enchantId}");
            return false;
        }
        return true;
    }

    private float GetValue(int enchantId, int level)
    {
        Legacy_CharacterRepo repo = GetRepo();
        Legacy_EnchantLevelData levelData = repo != null ? repo.GetEnchantLevel(enchantId, level) : null;
        if (levelData == null)
        {
            Debug.LogWarning($"[EnchantApplication] EnchantLevel 데이터 없음: {enchantId} Lv{level}");
            return 0f;
        }
        return levelData.Value;
    }

    private Legacy_CharacterRepo GetRepo()
    {
        // 인게임 인챈트 데이터는 Legacy_DataManager 경로를 사용한다
        // (EnchantSelectView/Presenter가 쓰는 것과 동일한 런타임 소스).
        return Legacy_DataManager.Instance != null ? Legacy_DataManager.Instance.CharacterRepo : null;
    }

    // ⚠️ INTERIM 매핑: 기획에서 stat-type enum이 확정되면 이 switch를 교체할 것.
    private void ApplyStat(int enchantId, int statType, float amount)
    {
        if (Mathf.Approximately(amount, 0f)) return;

        // [진단 로그] 인챈트 효과가 실제로 PlayerModel에 누적되는지 확인용 (원인 파악 후 제거 예정)
        float beforeAtk = _playerModel.Attack;
        int beforeHp = _playerModel.MaxHP;

        switch (statType)
        {
            case StatType_Attack:
                // 공격력 배율(%) 증가. base가 int라 Add(+0.1)는 반올림으로 묻히므로 Rate.
                _playerModel.Legacy_ApplyAttackBonus_Rate(amount);
                break;
            case StatType_Pierce:
                // PercentagePierce(관통)에 가산. (데모에선 관통 비활성이라 효과 안 보임)
                _playerModel.Legacy_ApplyPierceBonus_Add(amount);
                break;
            case StatType_MaxHP:
                // 최대 체력 정수 가산 (Value=100/200/300 등).
                _playerModel.Legacy_ApplyHpBonus_Add(Mathf.RoundToInt(amount));
                break;
            case StatType_CritRate:
                // 치명타 확률(0~1)에 소수 가산.
                _playerModel.Legacy_ApplyCriRateBonus_AddF(amount);
                break;
            case StatType_CritDmg:
                // 치명타 피해 보너스에 소수 가산.
                _playerModel.Legacy_ApplyCriDmgBonus_AddF(amount);
                break;
            default:
                Debug.LogWarning($"[EnchantApplication] 미정의 LinkedStatType={statType} (enchant {enchantId}). " +
                                 "효과 미적용 — 기획 stat-type enum 확정 필요.");
                break;
        }

        // [진단 로그] 적용 전/후 비교 — 이게 매 선택마다 누적(증가)되면 정상.
        Debug.Log($"[인챈트적용] id={enchantId} statType={statType} delta={amount:0.###} | " +
                  $"ATK {beforeAtk:0.##}→{_playerModel.Attack:0.##}, MaxHP {beforeHp}→{_playerModel.MaxHP}, " +
                  $"CritRate={_playerModel.CriticalRate:0.###}, CritDmg={_playerModel.CriticalDamage:0.##}");
    }
}
