// 담당자 : 김영찬
// 설명   : 인챈트 등장 가중치 계산 + 랜덤 선택 (기획 v1.04 반영)

// 수정자 : 김영찬
// 수정 내용 : 인첸트 리롤 시 기존 인첸트가 등장하지 않도록 수정

using System.Collections.Generic;
using UnityEngine;

public class EnchantSelector
{
    private readonly SpellRepo _repo;
    private readonly EnchantProbabilityConfig _config; // 인스펙터에서 받아올 설정값

    // 구현 완료된 원소만 인챈트 카드로 노출한다. Skill_ID 첫자리 = 원소(1=불 2=물 3=바람 4=번개 5=얼음).
    // 물(2xxxx)·얼음(5xxxx) 골격 구현 완료(데이터+발동+판정)로 노출. VFX·상태이상(슬로우/빙결)·이동장판은 폴리싱 예정.
    private static readonly HashSet<int> ImplementedSkillElements = new HashSet<int> { 1, 2, 3, 4, 5 };
    
    // 구현 완료된 스텟 인첸트만 인첸트 카드로 노출한다. HP회복(70201 ~ 70203)은 구현 안됨(아키텍쳐 상 킬 카운트 수집하는 기능이 없음)
    private static readonly HashSet<int> NotImplementedStatElements = new HashSet<int> { 70201, 70202, 70203 };

    // 생성자에서 Config를 받도록 수정
    public EnchantSelector(SpellRepo repo, EnchantProbabilityConfig config)
    {
        _repo = repo;
        _config = config;
    }

    public List<EnchantCandidate> GenerateChoices(EnchantModel playerModel, int pickCount = 3, HashSet<int> excludedNameIds = null)
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

        // 인스펙터의 수치를 곱하여 확률 계산
        float stHeldTotal = playerModel.GetHeldStatCount() > 0 ? _config.StatPoolBaseWeight * (_config.Stat_HeldWeight / 100f) : 0f;
        float stUnheldTotal = playerModel.GetHeldStatCount() > 0 ? _config.StatPoolBaseWeight * (_config.Stat_UnheldWeight / 100f) : _config.StatPoolBaseWeight;

        if (statHeld.Count == 0) { stUnheldTotal = _config.StatPoolBaseWeight; stHeldTotal = 0f; }
        if (statUnheld.Count == 0) { stHeldTotal = _config.StatPoolBaseWeight; stUnheldTotal = 0f; }

        float indStatHeld = statHeld.Count > 0 ? stHeldTotal / statHeld.Count : 0f;
        float indStatUnheld = statUnheld.Count > 0 ? stUnheldTotal / statUnheld.Count : 0f;

        for (int i = 0; i < statHeld.Count; i++) { statHeld[i].Weight = indStatHeld; finalPool.Add(statHeld[i]); }
        for (int i = 0; i < statUnheld.Count; i++) { statUnheld[i].Weight = indStatUnheld; finalPool.Add(statUnheld[i]); }
        
        // 리롤 배제 및 풀 고갈 방지 (가중치가 모두 부여된 최종 풀에서 걸러내기)
        if (excludedNameIds != null && excludedNameIds.Count > 0)
        {
            // 배제할 ID가 없는 애들만 남긴 임시 풀 생성
            var filteredPool = finalPool.FindAll(x => !excludedNameIds.Contains(x.Name_ID));
            
            // 남은 전체 스킬+스탯 종류가 뽑아야 할 개수(pickCount) 이상일 때만 제외 적용 (에러 방지)
            if (filteredPool.Count >= pickCount)
            {
                finalPool = filteredPool;
            }
            else
            {
                Debug.LogWarning("[EnchantSelector] 남은 인챈트 풀이 부족하여 리롤 중복 배제 로직을 무시합니다.");
            }
        }
        
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