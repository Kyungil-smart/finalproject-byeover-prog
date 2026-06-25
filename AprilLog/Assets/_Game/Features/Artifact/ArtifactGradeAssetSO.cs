using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 아티팩트 등급별 표시 이미지(슬롯/배경 스프라이트)를 한 곳에서 관리하는 SO.
//          Resources 폴더에 "ArtifactGradeAssets" 이름으로 두면 ArtifactGradeInfo 가 자동 로드한다.
//          등급 구분을 색상 대신 이미지로 표현한다.
[CreateAssetMenu(fileName = "ArtifactGradeAssets", menuName = "Data/Artifact Grade Assets")]
public class ArtifactGradeAssetSO : ScriptableObject
{
    [Header("등급별 슬롯/배경 이미지")]
    [SerializeField] private Sprite _rareSprite;
    [SerializeField] private Sprite _epicSprite;
    [SerializeField] private Sprite _legendarySprite;

    // 등급에 해당하는 슬롯/배경 스프라이트를 반환한다.
    public Sprite GetSlotSprite(ArtifactGrade grade)
    {
        switch (grade)
        {
            case ArtifactGrade.Rare: return _rareSprite;
            case ArtifactGrade.Epic: return _epicSprite;
            case ArtifactGrade.Legendary: return _legendarySprite;
            default: return _rareSprite;
        }
    }
}
