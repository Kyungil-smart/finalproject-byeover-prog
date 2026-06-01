// 담당자 : 정승우
// 설명   : 아웃게임 캐릭터 레벨업 -- 재화 소모 즉시 레벨업

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

// 수정자 : 정승우
// 수정내용 : ConfigRepo가 Inspector에 연결되지 않아도 DataManager에서 자동 참조

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경 및 이 스크립트의 연결을 new DataManager로 변경

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
 
    private void Awake()
    {
        ResolveRepository();
    }

    public bool CanLevelUp()
    {
        ResolveRepository();
        if (_configRepo == null)
        {
            Debug.LogError("[OutGameGrowthSystem] ConfigRepo를 찾을 수 없어 레벨업 가능 여부를 확인할 수 없습니다.");
            return false;
        }

        var data = _configRepo.GetOutLevel(_progress.CharacterLevel);
        if (data == null) return false;
        return _currency.CanAfford(data.RequiredGold, data.RequiredParchment);
    }
 
    public void LevelUp()
    {
        ResolveRepository();

        if (!CanLevelUp()) return;
 
        var data = _configRepo.GetOutLevel(_progress.CharacterLevel);
        if (data == null)
        {
            Debug.LogWarning("[OutGameGrowthSystem] OutLevel data is missing. LevelUp skipped.");
            return;
        }
 
        _currency.SpendGold(data.RequiredGold);
        _currency.SpendParchment(data.RequiredParchment);
        _progress.SetCharacterLevel(_progress.CharacterLevel + 1);
 
        if (GameManager.Instance != null)
            GameManager.Instance.SyncToCloud(null);
 
        OnCharacterLevelUp?.Invoke(_progress.CharacterLevel);
    }

    private void ResolveRepository()
    {
        if (_configRepo != null) return;
        if (DataManager.Instance == null) return;

        _configRepo = DataManager.Instance.ConfigRepo;
    }
}
