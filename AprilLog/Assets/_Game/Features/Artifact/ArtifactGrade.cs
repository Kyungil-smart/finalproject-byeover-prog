using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 아티팩트 등급 enum과 UI 표시용 헬퍼(표시명/슬롯 색상)
//          E = 레어, R = 에픽, L = 레전더리 (데이터 등급 코드 -> enum 변환은 데이터 연동 담당 영역)

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

    // 슬롯 배경색
    public static Color SlotColor(ArtifactGrade grade)
    {
        switch (grade)
        {
            case ArtifactGrade.Rare: return Hex("#4A90E2");       // 스카이블루
            case ArtifactGrade.Epic: return Hex("#9B51E0");       // 로열 퍼플
            case ArtifactGrade.Legendary: return Hex("#F2C94C");  // 하이퍼 골드
            default: return Color.white;
        }
    }

    private static Color Hex(string hex)
    {
        return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
    }
}
