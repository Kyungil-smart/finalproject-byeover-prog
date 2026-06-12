// 담당자 : 정승우
// 설명   : 스킬 테스트 씬(Skill_TEST) 전용 스테이지 튜닝.
//          이 컴포넌트가 "씬에 있을 때만" StageBootstrapper가 웨이브 룰에 배율을 적용한다.
//          정식 씬(_InGame 등)에는 이 컴포넌트가 없으므로 아무 영향 없음.
//          공용 SO/원본 룰은 복제본으로 보호된다 (절대 직접 수정하지 않음).

using System.Collections.Generic;
using UnityEngine;

public class SkillTestStageTuning : MonoBehaviour
{
    [Header("스폰 튜닝 (Skill_TEST 전용)")]
    [Tooltip("스폰 간격 배율. 0.5 = 5초 → 2.5초")]
    [SerializeField] private float _spawnIntervalMultiplier = 0.5f;

    [Tooltip("스폰 수량 배율. 2 = 두 배")]
    [SerializeField] private float _spawnAmountMultiplier = 2f;

    /// <summary>웨이브 룰을 복제해 튜닝을 적용한 새 리스트를 돌려준다 (원본/SO 무손상).</summary>
    public List<StageWaveRuleData> ApplyTuning(List<StageWaveRuleData> rules)
    {
        if (rules == null) return null;

        var tuned = new List<StageWaveRuleData>(rules.Count);
        for (int i = 0; i < rules.Count; i++)
        {
            var clone = Clone(rules[i]);
            clone.SpawnInterval = rules[i].SpawnInterval * _spawnIntervalMultiplier;
            clone.SpawnAmount = Mathf.Max(1, Mathf.RoundToInt(rules[i].SpawnAmount * _spawnAmountMultiplier));
            tuned.Add(clone);
        }

        Debug.Log($"[SkillTest] 스테이지 튜닝 적용: 스폰 간격 x{_spawnIntervalMultiplier}, 스폰 수량 x{_spawnAmountMultiplier} ({tuned.Count}개 웨이브 룰)");
        return tuned;
    }

    private static StageWaveRuleData Clone(StageWaveRuleData src)
    {
        return new StageWaveRuleData
        {
            Stage_ID = src.Stage_ID,
            WaveOrder = src.WaveOrder,
            WaveDuration = src.WaveDuration,
            WaveEndType = src.WaveEndType,
            WaveEndAction = src.WaveEndAction,
            SpawnInterval = src.SpawnInterval,
            SpawnAmount = src.SpawnAmount,
            MonsterWavePool_ID = src.MonsterWavePool_ID,
            NormalChance = src.NormalChance,
            AgileChance = src.AgileChance,
            TankChance = src.TankChance,
            RangedChance = src.RangedChance,
            InfestedChance = src.InfestedChance,
            SpecialWave_ID = src.SpecialWave_ID,
        };
    }
}
