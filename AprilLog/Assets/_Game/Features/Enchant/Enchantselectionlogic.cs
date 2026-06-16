// 담당자 : 정승우
// 설명   : 인챈트 등장 가중치 계산 + 랜덤 선택

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업 시 인챈트 3종 선택지를 생성한다.
/// 보유 인챈트에 따라 가중치가 바뀜.
/// 결정론적 RNG로 리세마라 방지.
/// </summary>
public class EnchantSelectionLogic
{
    private readonly Legacy_CharacterRepo _repo;
    private readonly EnchantModel _model;
    private readonly System.Random _rng;

    // 카드 등장 루트: 레벨업마다 3장이 '한 타입'으로, 스킬→패시브→패시브→스킬 4주기 순환 (true=스킬, false=패시브).
    private static readonly bool[] SkillRoute = { true, false, false, true };
    private int _routeIndex;  // 세션 내 레벨업 순번 (⚠ 이어하기 시 0으로 리셋되는 한계 — 영구화하려면 세이브 연동 필요)

    public EnchantSelectionLogic(Legacy_CharacterRepo repo, EnchantModel model, System.Random rng)
    {
        _repo = repo;
        _model = model;
        _rng = rng;
    }

    // 인챈트 3종 선택지 생성
    public List<Legacy_EnchantMasterData> GenerateChoices(bool advanceRoute = true)
    {
        var result = new List<Legacy_EnchantMasterData>();
        var allEnchants = _repo.GetAllEnchantMasters();

        // 이번 레벨업의 카드 타입 결정 (루트 4주기 순환). true=스킬, false=패시브(스탯).
        // 리롤(advanceRoute=false)이면 직전 레벨업과 같은 타입을 유지하고 순번을 올리지 않는다.
        int routeAt = advanceRoute ? _routeIndex : Mathf.Max(0, _routeIndex - 1);
        bool wantSkill = SkillRoute[routeAt % SkillRoute.Length];
        if (advanceRoute) _routeIndex++;

        var owned = new List<Legacy_EnchantMasterData>();
        var unowned = new List<Legacy_EnchantMasterData>();

        // 1차(pass 0): 이번 루트 타입만 후보. 후보가 0이면 2차(pass 1)에서 타입 무시하고 전체에서 — 빈 선택지 방지.
        for (int pass = 0; pass < 2; pass++)
        {
            owned.Clear();
            unowned.Clear();
            bool applyTypeFilter = (pass == 0);

            foreach (var pair in allEnchants)
            {
                var data = pair.Value;

                // 최대 레벨이면 후보에서 제외
                if (_model.GetEnchantLevel(data.EnchantID) >= data.MaxLevel) continue;

                // 루트 타입 필터 (스킬: LinkedSkillID>0 / 패시브: LinkedSkillID<=0)
                if (applyTypeFilter && (data.LinkedSkillID > 0) != wantSkill) continue;

                if (_model.HasEnchant(data.EnchantID))
                    owned.Add(data);
                else
                    unowned.Add(data);
            }

            if (owned.Count > 0 || unowned.Count > 0) break; // 후보 확보됨 — 폴백 불필요
        }

        // 3개 뽑기
        for (int i = 0; i < 3; i++)
        {
            var picked = PickOne(owned, unowned);
            if (picked != null)
                result.Add(picked);
        }

        return result;
    }

    // 보유 인챈트가 많을수록 보유군에서 뽑힐 확률이 올라감
    private Legacy_EnchantMasterData PickOne(List<Legacy_EnchantMasterData> owned, List<Legacy_EnchantMasterData> unowned)
    {
        if (owned.Count == 0 && unowned.Count == 0) return null;
        if (owned.Count == 0) return PickRandom(unowned);
        if (unowned.Count == 0) return PickRandom(owned);

        // 보유 개수에 따른 확률 조정
        float ownedProb;
        int ownedCount = _model.GetOwnedCount();

        if (ownedCount <= 2)
            ownedProb = 0.3f;       // 초반에는 미보유 위주
        else if (ownedCount <= 4)
            ownedProb = 0.5f;       // 중반에는 반반
        else
            ownedProb = 0.8f;       // 후반에는 보유 레벨업 위주

        float roll = (float)_rng.NextDouble();
        if (roll < ownedProb)
            return PickRandom(owned);
        else
            return PickRandom(unowned);
    }

    // 뽑은 항목을 후보 리스트에서 제거하고 반환(복원 없는 추출) — 한 번의 선택지(3장)에 같은 인챈트 중복 방지.
    // owned/unowned는 GenerateChoices가 매번 새로 만든 로컬 사본이라 제거해도 원본 데이터엔 영향 없음.
    private Legacy_EnchantMasterData PickRandom(List<Legacy_EnchantMasterData> list)
    {
        if (list.Count == 0) return null;
        int idx = _rng.Next(0, list.Count);
        var picked = list[idx];
        list.RemoveAt(idx);
        return picked;
    }
}