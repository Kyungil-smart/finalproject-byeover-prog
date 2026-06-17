// 담당자 : 김영찬
// 설명   : 인챈트 등장 가중치 계산 + 랜덤 선택 (기획 v1.04 반영)

using System.Collections.Generic;

public class EnchantSelector
{
    private readonly SpellRepo _repo;
    private readonly EnchantProbabilityConfig _config; // 인스펙터에서 받아올 설정값

    // 구현 완료된 원소만 인챈트 카드로 노출한다. Skill_ID 첫자리 = 원소(1=불 2=물 3=바람 4=번개 5=얼음).
    // 물(2xxxx)·얼음(5xxxx)은 실행 스킬·VFX가 아직 없어 뽑아도 발동이 안 되므로 풀에서 제외.
    // → 물/얼음을 구현하면 여기에 원소 숫자(2, 5)만 추가하면 카드가 다시 등장한다.
    private static readonly HashSet<int> ImplementedSkillElements = new HashSet<int> { 1, 3, 4 };

    // 생성자에서 Config를 받도록 수정
    public EnchantSelector(SpellRepo repo, EnchantProbabilityConfig config)
    {
        _repo = repo;
        _config = config;
    }

    public List<EnchantCandidate> GenerateChoices(EnchantModel playerModel, int pickCount = 3)
    {
        List<EnchantCandidate> finalPool = new List<EnchantCandidate>();
        
        // 스킬 후보군 수집 및 가중치 계산
        List<EnchantCandidate> skillHeld = new List<EnchantCandidate>();
        List<EnchantCandidate> skillUnheld = new List<EnchantCandidate>();

        foreach (var group in _repo.GetAllSkillGroups().Values)
        {
            foreach (var chain in group.SkillNameChainData.Values)
            {
                int currentLv = playerModel.GetSkillLevel(chain.Name_ID);
                var nextData = chain.GetNextLevelData(currentLv);
                if (nextData == null) continue;

                // 미구현 원소(물 2xxxx·얼음 5xxxx)와 자동공격 베이스(60005/60010 → 첫자리 6)는 카드 후보에서 제외.
                if (!ImplementedSkillElements.Contains(nextData.Skill_ID / 10000)) continue;

                var candidate = new EnchantCandidate
                {
                    Type = EnchantType.Skill, Name_ID = chain.Name_ID, Specific_ID = nextData.Skill_ID,
                    Level = nextData.Level, SkillData = nextData
                };

                if (currentLv > 0) skillHeld.Add(candidate);
                else skillUnheld.Add(candidate);
            }
        }

        float sHeldTotal = 0f, sUnheldTotal = 0f;
        int heldSkillCount = playerModel.GetHeldSkillCount();

        // 인스펙터의 수치를 곱하여 확률 계산
        if (heldSkillCount == 0) 
        { 
            sUnheldTotal = _config.SkillPoolBaseWeight; 
        }
        else if (heldSkillCount <= 2) 
        { 
            sHeldTotal = _config.SkillPoolBaseWeight * (_config.SkillStage1_HeldWeight / 100f); 
            sUnheldTotal = _config.SkillPoolBaseWeight * (_config.SkillStage1_UnheldWeight / 100f); 
        }
        else if (heldSkillCount <= 4) 
        { 
            sHeldTotal = _config.SkillPoolBaseWeight * (_config.SkillStage2_HeldWeight / 100f); 
            sUnheldTotal = _config.SkillPoolBaseWeight * (_config.SkillStage2_UnheldWeight / 100f); 
        }
        else 
        { 
            sHeldTotal = _config.SkillPoolBaseWeight * (_config.SkillStage3_HeldWeight / 100f); 
            sUnheldTotal = _config.SkillPoolBaseWeight * (_config.SkillStage3_UnheldWeight / 100f); 
        }

        if (skillHeld.Count == 0) { sUnheldTotal = _config.SkillPoolBaseWeight; sHeldTotal = 0f; }
        if (skillUnheld.Count == 0) { sHeldTotal = _config.SkillPoolBaseWeight; sUnheldTotal = 0f; }

        float indSkillHeld = skillHeld.Count > 0 ? sHeldTotal / skillHeld.Count : 0f;
        float indSkillUnheld = skillUnheld.Count > 0 ? sUnheldTotal / skillUnheld.Count : 0f;

        for (int i = 0; i < skillHeld.Count; i++) { skillHeld[i].Weight = indSkillHeld; finalPool.Add(skillHeld[i]); }
        for (int i = 0; i < skillUnheld.Count; i++) { skillUnheld[i].Weight = indSkillUnheld; finalPool.Add(skillUnheld[i]); }

        
        // 스탯 후보군 수집 및 가중치 계산
        List<EnchantCandidate> statHeld = new List<EnchantCandidate>();
        List<EnchantCandidate> statUnheld = new List<EnchantCandidate>();

        foreach (var group in _repo.GetAllStatGroups().Values)
        {
            foreach (var chain in group.StatNameChainData.Values)
            {
                int currentLv = playerModel.GetStatLevel(chain.Stat_Name_ID);
                var nextData = chain.GetNextLevelData(currentLv);
                if (nextData == null) continue;

                var candidate = new EnchantCandidate
                {
                    Type = EnchantType.Stat, Name_ID = chain.Stat_Name_ID, Specific_ID = nextData.StatEnchant_ID,
                    Level = nextData.StatLevel, StatData = nextData
                };

                if (currentLv > 0) statHeld.Add(candidate);
                else statUnheld.Add(candidate);
            }
        }

        // 인스펙터의 수치를 곱하여 확률 계산
        float stHeldTotal = playerModel.GetHeldStatCount() > 0 ? _config.StatPoolBaseWeight * (_config.Stat_HeldWeight / 100f) : 0f;
        float stUnheldTotal = playerModel.GetHeldStatCount() > 0 ? _config.StatPoolBaseWeight * (_config.Stat_UnheldWeight / 100f) : _config.StatPoolBaseWeight;

        if (statHeld.Count == 0) { stUnheldTotal = _config.StatPoolBaseWeight; stHeldTotal = 0f; }
        if (statUnheld.Count == 0) { stHeldTotal = _config.StatPoolBaseWeight; stUnheldTotal = 0f; }

        float indStatHeld = statHeld.Count > 0 ? stHeldTotal / statHeld.Count : 0f;
        float indStatUnheld = statUnheld.Count > 0 ? stUnheldTotal / statUnheld.Count : 0f;

        for (int i = 0; i < statHeld.Count; i++) { statHeld[i].Weight = indStatHeld; finalPool.Add(statHeld[i]); }
        for (int i = 0; i < statUnheld.Count; i++) { statUnheld[i].Weight = indStatUnheld; finalPool.Add(statUnheld[i]); }

        // pickCount만큼 뽑기
        return GetWeightedRandomPicks(finalPool, pickCount);
    }

    private List<EnchantCandidate> GetWeightedRandomPicks(List<EnchantCandidate> pool, int count)
    {
        List<EnchantCandidate> results = new List<EnchantCandidate>();
        List<EnchantCandidate> tempPool = new List<EnchantCandidate>(pool);

        for (int i = 0; i < count; i++)
        {
            if (tempPool.Count == 0) break;
            float totalWeight = 0f;
            for (int w = 0; w < tempPool.Count; w++) totalWeight += tempPool[w].Weight;

            float randomVal = UnityEngine.Random.Range(0f, totalWeight);
            float cursor = 0f;

            for (int j = 0; j < tempPool.Count; j++)
            {
                cursor += tempPool[j].Weight;
                if (randomVal <= cursor)
                {
                    results.Add(tempPool[j]);
                    tempPool.RemoveAt(j); 
                    break;
                }
            }
        }
        return results;
    }
}