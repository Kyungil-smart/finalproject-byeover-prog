using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 아티팩트 등급 enum과 UI 표시용 헬퍼(표시명/슬롯 이미지)
//          E = 레어, R = 에픽, L = 레전더리 (데이터 등급 코드 -> enum 변환은 데이터 연동 담당 영역)
//          등급 구분은 색상이 아니라 이미지로 한다. 이미지는 Resources/ArtifactGradeAssets(ArtifactGradeAssetSO)에서 관리.

// enum 값 순서 = 등급 낮은순(레어 < 에픽 < 레전더리). 정렬 시 그대로 비교에 사용
public enum ArtifactGrade
{
    Rare = 0,
    Epic = 1,
    Legendary = 2
}

public static class ArtifactGradeInfo
{
    public static string DisplayName(ArtifactGrade grade) => grade switch
    {
        ArtifactGrade.Rare => "레어",
        ArtifactGrade.Epic => "에픽",
        ArtifactGrade.Legendary => "레전더리",
        _ => "레어"
    };

    // Resources 폴더 기준 경로(확장자 없음). Resources/ArtifactGradeAssets.asset
    public const string AssetResourcePath = "ArtifactGradeAssets";

    private static ArtifactGradeAssetSO _assets;
    private static bool _warned;

    private static ArtifactGradeAssetSO Assets
    {
        get
        {
            if (_assets == null)
            {
                _assets = Resources.Load<ArtifactGradeAssetSO>(AssetResourcePath);
                if (_assets == null && !_warned)
                {
                    _warned = true;
                    Debug.LogWarning($"[ArtifactGradeInfo] Resources/{AssetResourcePath} (ArtifactGradeAssetSO) 를 찾지 못했습니다. 등급 이미지가 표시되지 않습니다.");
                }
            }
            return _assets;
        }
    }

    // 등급별 슬롯/배경 이미지. (색상 대신 이미지로 등급 구분)
    public static Sprite SlotSprite(ArtifactGrade grade)
    {
        return Assets != null ? Assets.GetSlotSprite(grade) : null;
    }
}
