// 담당자 : 정승우
// 설명   : 아웃게임 캐릭터 레벨업 -- 재화 소모 즉시 레벨업

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

using System;
using UnityEngine;
 
/// <summary>
/// 로비에서 골드 + 양피지를 소모해서 캐릭터를 레벨업한다.
/// </summary>
public class OutGameGrowthSystem : MonoBehaviour
{
    public event Action<int> OnCharacterLevelUp;
 
    [SerializeField] private ConfigRepo _configRepo;
    [SerializeField] private CurrencyModel _currency;
    [SerializeField] private PlayerProgressModel _progress;
 
    public bool CanLevelUp()
    {
        if (_configRepo == null || _currency == null || _progress == null)
        {
            Debug.LogWarning("[OutGameGrowthSystem] Required dependency is missing. CanLevelUp returns false.");
            return false;
        }

        var data = _configRepo.GetOutLevel(_progress.CharacterLevel);
        if (data == null) return false;
        return _currency.CanAfford(data.ConsumeGold, data.ConsumeParchment);
    }
 
    public void LevelUp()
    {
        if (!CanLevelUp()) return;
 
        var data = _configRepo.GetOutLevel(_progress.CharacterLevel);
        if (data == null)
        {
            Debug.LogWarning("[OutGameGrowthSystem] OutLevel data is missing. LevelUp skipped.");
            return;
        }
 
        _currency.SpendGold(data.ConsumeGold);
        _currency.SpendParchment(data.ConsumeParchment);
        _progress.SetCharacterLevel(_progress.CharacterLevel + 1);
 
        if (GameManager.Instance != null)
            GameManager.Instance.SyncToCloud(null);
 
        OnCharacterLevelUp?.Invoke(_progress.CharacterLevel);
    }
}
