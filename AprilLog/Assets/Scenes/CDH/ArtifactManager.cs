
using System.Collections.Generic;
using UnityEngine;

public class ArtifactManager : MonoBehaviour
{
    public List<ArtifactInstance> MyArtifacts = new List<ArtifactInstance>();

    public void Initialize()
    {
        Debug.Log("[ArtifactManager] 초기화 완료. 데이터를 로드할 준비가 되었습니다.");
    }

    public void AddArtifact(int masterId)
    {
        int newUniqueId = GenerateNewUniqueId();

        ArtifactInstance newArtifact = new ArtifactInstance();
        newArtifact.UniqueId = newUniqueId;
        newArtifact.MasterId = masterId;
        newArtifact.CurrentLevel = 1;
        newArtifact.IsEquipped = false;

        MyArtifacts.Add(newArtifact);
    }

    public void RequestUpgrade(int uniqueId)
    {
        var artifact = MyArtifacts.Find(a => a.UniqueId == uniqueId);

        if (artifact != null && artifact.CanLevelUp())
        {
            artifact.CurrentLevel++;
            Debug.Log($"[인벤토리] {artifact.UniqueId} 강화 성공! 레벨: {artifact.CurrentLevel}");
        }
    }

    private int GenerateNewUniqueId() { return Random.Range(1000, 9999); }
}

