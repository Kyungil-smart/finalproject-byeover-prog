// 작성자 : 김영찬
// 설명 : 스킬과 스탯이 분리되어 순서대로 등장할 때 사용하는 전용 셀렉터

// 수정자 : 김영찬
// 수정 내용 : 인첸트 리롤 시 기존 인첸트가 등장하지 않도록 수정

using System.Collections.Generic;
using UnityEngine;

public class EnchantSequenceSelector
{
    private readonly SpellRepo _repo;
    private readonly EnchantProbabilityConfig _config;
    
    // 구현 완료된 원소만 인챈트 카드로 노출한다. Skill_ID 첫자리 = 원소(1=불 2=물 3=바람 4=번개 5=얼음).
    // 물(2xxxx)·얼음(5xxxx) 골격 구현 완료(데이터+발동+판정)로 노출. VFX·상태이상(슬로우/빙결)·이동장판은 폴리싱 예정.
    private static readonly HashSet<int> ImplementedSkillElements = new HashSet<int> { 1, 2, 3, 4, 5 };
    
    // 구현 완료된 스텟 인첸트만 인첸트 카드로 노출한다. HP회복(70201 ~ 70203)은 구현 안됨(아키텍쳐 상 킬 카운트 수집하는 기능이 없음)
    private static readonly HashSet<int> NotImplementedStatElements = new HashSet<int> { 70201, 70202, 70203 };

    public EnchantSequenceSelector(SpellRepo repo, EnchantProbabilityConfig config)
    {
        _repo = repo;
        _config = config;
    }
    
    // ---------- 인첸트 선택 로직 ----------
    // 스킬 전용 픽
    public List<EnchantCandidate> GenerateSkillChoices(EnchantModel playerModel, int pickCount = 3, List<int> excludedNameIds = null)
    {
        List<EnchantCandidate> finalPool = new List<EnchantCandidate>();
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
        
        // 리롤 배제 및 안전장치
        if (excludedNameIds != null && excludedNameIds.Count > 0)
        {
            // 제외하고 나서 남는 스킬이 몇 개인지 미리 계산
            int remainCount = skillHeld.FindAll(x => !excludedNameIds.Contains(x.Name_ID)).Count + 
                              skillUnheld.FindAll(x => !excludedNameIds.Contains(x.Name_ID)).Count;

            // 남은 스킬이 뽑아야 할 개수(pickCount)보다 많거나 같을 때만 배제 실행
            if (remainCount >= pickCount)
            {
                skillHeld.RemoveAll(x => excludedNameIds.Contains(x.Name_ID));
                skillUnheld.RemoveAll(x => excludedNameIds.Contains(x.Name_ID));
            }
            else
            {
                Debug.LogWarning("[EnchantSelector] 남은 스킬 풀이 부족하여 리롤 중복 배제 로직을 무시합니다.");
            }
        }

        float sHeldTotal = 0f, sUnheldTotal = 0f;
        int heldSkillCount = playerModel.GetHeldSkillCount();

        if (heldSkillCount == 0) { sUnheldTotal = 100f; }
        else if (heldSkillCount <= 2) { sHeldTotal = _config.SkillStage1_HeldWeight; sUnheldTotal = _config.SkillStage1_UnheldWeight; }
        else if (heldSkillCount <= 4) { sHeldTotal = _config.SkillStage2_HeldWeight; sUnheldTotal = _config.SkillStage2_UnheldWeight; }
        else { sHeldTotal = _config.SkillStage3_HeldWeight; sUnheldTotal = _config.SkillStage3_UnheldWeight; }

        if (skillHeld.Count == 0) { sUnheldTotal = 100f; sHeldTotal = 0f; }
        if (skillUnheld.Count == 0) { sHeldTotal = 100f; sUnheldTotal = 0f; }

        float indSkillHeld = skillHeld.Count > 0 ? sHeldTotal / skillHeld.Count : 0f;
        float indSkillUnheld = skillUnheld.Count > 0 ? sUnheldTotal / skillUnheld.Count : 0f;

        for (int i = 0; i < skillHeld.Count; i++) { skillHeld[i].Weight = indSkillHeld; finalPool.Add(skillHeld[i]); }
        for (int i = 0; i < skillUnheld.Count; i++) { skillUnheld[i].Weight = indSkillUnheld; finalPool.Add(skillUnheld[i]); }

        return GetWeightedRandomPicks(finalPool, pickCount);
    }
    
    // 스탯 전용 픽
    public List<EnchantCandidate> GenerateStatChoices(EnchantModel playerModel, int pickCount = 3, List<int> excludedNameIds = null)
    {
        List<EnchantCandidate> finalPool = new List<EnchantCandidate>();
        List<EnchantCandidate> statHeld = new List<EnchantCandidate>();
        List<EnchantCandidate> statUnheld = new List<EnchantCandidate>();

        foreach (var group in _repo.GetAllStatGroups().Values)
        {
            foreach (var chain in group.StatNameChainData.Values)
            {
                int currentLv = playerModel.GetStatLevel(chain.Stat_Name_ID);
                var nextData = chain.GetNextLevelData(currentLv);
                if (nextData == null) continue;
                
                // 미구현 스텟 인첸트는 선택에서 제외
                if (NotImplementedStatElements.Contains(nextData.StatEnchant_ID)) continue;

                var candidate = new EnchantCandidate
                {
                    Type = EnchantType.Stat, Name_ID = chain.Stat_Name_ID, Specific_ID = nextData.StatEnchant_ID,
                    Level = nextData.StatLevel, StatData = nextData
                };

                if (currentLv > 0) statHeld.Add(candidate);
                else statUnheld.Add(candidate);
            }
        }
        
        // 리롤 배제 및 안전장치
        if (excludedNameIds != null && excludedNameIds.Count > 0)
        {
            int remainCount = statHeld.FindAll(x => !excludedNameIds.Contains(x.Name_ID)).Count + 
                              statUnheld.FindAll(x => !excludedNameIds.Contains(x.Name_ID)).Count;

            if (remainCount >= pickCount)
            {
                statHeld.RemoveAll(x => excludedNameIds.Contains(x.Name_ID));
                statUnheld.RemoveAll(x => excludedNameIds.Contains(x.Name_ID));
            }
            else
            {
                Debug.LogWarning("[EnchantSelector] 남은 스텟 인첸트 풀이 부족하여 리롤 중복 배제 로직을 무시합니다.");
            }
        }

        float stHeldTotal = playerModel.GetHeldStatCount() > 0 ? _config.Stat_HeldWeight : 0f;
        float stUnheldTotal = playerModel.GetHeldStatCount() > 0 ? _config.Stat_UnheldWeight : 100f;

        if (statHeld.Count == 0) { stUnheldTotal = 100f; stHeldTotal = 0f; }
        if (statUnheld.Count == 0) { stHeldTotal = 100f; stUnheldTotal = 0f; }

        float indStatHeld = statHeld.Count > 0 ? stHeldTotal / statHeld.Count : 0f;
        float indStatUnheld = statUnheld.Count > 0 ? stUnheldTotal / statUnheld.Count : 0f;

        for (int i = 0; i < statHeld.Count; i++) { statHeld[i].Weight = indStatHeld; finalPool.Add(statHeld[i]); }
        for (int i = 0; i < statUnheld.Count; i++) { statUnheld[i].Weight = indStatUnheld; finalPool.Add(statUnheld[i]); }

        return GetWeightedRandomPicks(finalPool, pickCount);
    }

    // ---------- 보조 함수 ----------
    private List<EnchantCandidate> GetWeightedRandomPicks(List<EnchantCandidate> pool, int count)
    {
        List<EnchantCandidate> results = new List<EnchantCandidate>();
        List<EnchantCandidate> tempPool = new List<EnchantCandidate>(pool);

        for (int i = 0; i < count; i++)
        {
            if (tempPool.Count == 0) break;
            float totalWeight = 0f;
            for (int w = 0; w < tempPool.Count; w++) totalWeight += tempPool[w].Weight;

            float randomVal = Random.Range(0f, totalWeight);
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