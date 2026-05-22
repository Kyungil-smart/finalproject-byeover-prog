// 담당자 : 정승우
// 설명   : 아웃게임 캐릭터 레벨업 -- 재화 소모 즉시 레벨업

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

using System;
using UnityEngine;

/// <summary>
/// 로비에서 양피지 + 골드를 소모해서 캐릭터를 레벨업한다.
/// 경험치 없이 재화만 있으면 바로 올라감.
/// </summary>
public class OutGameGrowthSystem : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnCharacterLevelUp;

    // ---------- SerializeField ----------
    [SerializeField] private CurrencyModel _currency;
    [SerializeField] private PlayerProgressModel _progress;

    // ---------- Public ----------
    public bool CanLevelUp()
    {
        var data = DataManager.Instance.ConfigRepo.GetOutGrowth(_progress.CharacterLevel);
        if (data == null) return false;

        return _currency.CanAfford(data.RequiredGold, data.RequiredParchment);
    }

    public void LevelUp()
    {
        if (!CanLevelUp()) return;

        var data = DataManager.Instance.ConfigRepo.GetOutGrowth(_progress.CharacterLevel);

        _currency.SpendGold(data.RequiredGold);
        _currency.SpendParchment(data.RequiredParchment);

        _progress.SetCharacterLevel(_progress.CharacterLevel + 1);

        if (GameManager.Instance != null)
            GameManager.Instance.SyncToCloud(null);

        OnCharacterLevelUp?.Invoke(_progress.CharacterLevel);
    }
}