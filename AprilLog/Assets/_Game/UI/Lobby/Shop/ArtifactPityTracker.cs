using UnityEngine;

// 작성자 : 홍정옥
// 설명 : 가챠 천장(레전더리 확정) 카운트를 관리한다. (누적/마일리지 카운트와는 완전히 별개)
// 기획서 6-2-1 :
//  - 레전더리 상자 1회 개봉마다 카운트 +1
//  - 레전더리 아티팩트가 등장하면 즉시 카운트 0으로 초기화
//  - 카운트가 천장(PityCount)에 도달하면 그 개봉에서 100% 레전더리 확정
// 카운트는 가챠별로 PlayerPrefs 에 영구 저장한다.
public class ArtifactPityTracker : MonoBehaviour
{
    private const string PityKeyPrefix = "Artifact_Pity_Count_";

    private string Key(int gachaId) => PityKeyPrefix + gachaId;

    // 마지막 레전더리 이후 누적 개봉 수(0..PityCount-1). 다음 개봉이 PityCount 번째면 확정.
    public int GetCount(int gachaId) => PlayerPrefs.GetInt(Key(gachaId), 0);

    public void SetCount(int gachaId, int value)
    {
        PlayerPrefs.SetInt(Key(gachaId), Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    public void Reset(int gachaId) => SetCount(gachaId, 0);
}
