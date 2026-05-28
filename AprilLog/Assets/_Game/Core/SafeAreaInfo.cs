// 담당자 : 조규민
// 설명   : Safe Area 계산 결과 값
// 수정자 : 정승우
// 수정내용 : Safe Area API 분리를 위한 정보 구조체 추가

using UnityEngine;

public readonly struct SafeAreaInfo
{
    public readonly Rect PixelRect;
    public readonly Vector2 AnchorMin;
    public readonly Vector2 AnchorMax;
    public readonly Vector4 Insets;
    public readonly Vector2Int ScreenSize;

    public bool IsFullScreen => Insets == Vector4.zero;

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
