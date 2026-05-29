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

    public EnchantSelectionLogic(Legacy_CharacterRepo repo, EnchantModel model, System.Random rng)
    {
        _repo = repo;
        _model = model;
        _rng = rng;
    }

    // 인챈트 3종 선택지 생성
    public List<EnchantMasterData> GenerateChoices()
    {
        var result = new List<EnchantMasterData>();
        var allEnchants = _repo.GetAllEnchantMasters();

        // 후보 분리
        var owned = new List<EnchantMasterData>();
        var unowned = new List<EnchantMasterData>();

        foreach (var pair in allEnchants)
        {
            var data = pair.Value;

            // 최대 레벨이면 후보에서 제외
            int currentLv = _model.GetEnchantLevel(data.EnchantID);
            if (currentLv >= data.MaxLevel) continue;

            if (_model.HasEnchant(data.EnchantID))
                owned.Add(data);
            else
                unowned.Add(data);
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
    private EnchantMasterData PickOne(List<EnchantMasterData> owned, List<EnchantMasterData> unowned)
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

    private EnchantMasterData PickRandom(List<EnchantMasterData> list)
    {
        if (list.Count == 0) return null;
        return list[_rng.Next(0, list.Count)];
    }
}