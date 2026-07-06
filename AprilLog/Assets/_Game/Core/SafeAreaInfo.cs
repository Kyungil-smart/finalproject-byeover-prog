// 담당자 : 조규민
// Safe Area 계산 결과와 화면 크기 정보를 함께 전달하는 불변 데이터
// 설명   : Safe Area 계산 결과 값
// 수정자 : 정승우
// 수정내용 : Safe Area API 분리를 위한 정보 구조체 추가

using UnityEngine;

public readonly struct SafeAreaInfo
{
    // 실제 기기 화면 픽셀 기준으로 계산된 Safe Area 영역이다.
    public readonly Rect PixelRect;
    // RectTransform에 적용할 anchor 최소/최대 값이다.
    public readonly Vector2 AnchorMin;
    public readonly Vector2 AnchorMax;
    // 좌, 하, 우, 상 순서의 화면 가장자리 여백 값이다.
    public readonly Vector4 Insets;
    // Safe Area 계산에 사용한 화면 크기다.
    public readonly Vector2Int ScreenSize;

    public bool IsFullScreen => Insets == Vector4.zero;

    // Safe Area 계산 결과를 불변 값으로 묶어 전달한다.
    public SafeAreaInfo(
        Rect pixelRect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector4 insets,
        Vector2Int screenSize)
    {
        PixelRect = pixelRect;
        AnchorMin = anchorMin;
        AnchorMax = anchorMax;
        Insets = insets;
        ScreenSize = screenSize;
    }
}
