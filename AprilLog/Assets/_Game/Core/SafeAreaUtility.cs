// 담당자 : 조규민
// 설명   : Safe Area 계산 API
// 수정자 : 정승우
// 수정내용 : Screen.safeArea를 UI anchor와 inset 정보로 변환하는 유틸리티 추가

using UnityEngine;

public static class SafeAreaUtility
{
    // 현재 기기의 Screen.safeArea를 지정된 가장자리 옵션에 맞춰 SafeAreaInfo로 변환한다.
    public static SafeAreaInfo GetCurrentInfo(SafeAreaEdges edges = SafeAreaEdges.All)
    {
        return CreateInfo(Screen.safeArea, Screen.width, Screen.height, edges);
    }

    // 입력받은 Safe Area 픽셀 Rect를 UI Anchor와 Insets 값으로 변환한다.
    public static SafeAreaInfo CreateInfo(Rect safeArea, int screenWidth, int screenHeight, SafeAreaEdges edges)
    {
        Rect fullArea = new Rect(0f, 0f, screenWidth, screenHeight);
        Rect appliedArea = ApplyEdges(safeArea, fullArea, edges);

        Vector2 anchorMin = appliedArea.position;
        Vector2 anchorMax = appliedArea.position + appliedArea.size;

        if (screenWidth > 0)
        {
            anchorMin.x /= screenWidth;
            anchorMax.x /= screenWidth;
        }

        if (screenHeight > 0)
        {
            anchorMin.y /= screenHeight;
            anchorMax.y /= screenHeight;
        }

        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp01(anchorMax.x);
        anchorMax.y = Mathf.Clamp01(anchorMax.y);

        Vector4 insets = new Vector4(
            appliedArea.xMin,
            appliedArea.yMin,
            fullArea.xMax - appliedArea.xMax,
            fullArea.yMax - appliedArea.yMax);

        return new SafeAreaInfo(
            appliedArea,
            anchorMin,
            anchorMax,
            insets,
            new Vector2Int(screenWidth, screenHeight));
    }

    // 계산된 Safe Area Anchor를 RectTransform에 적용한다.
    public static void ApplyTo(RectTransform rect, SafeAreaInfo info)
    {
        if (rect == null) return;

        rect.anchorMin = info.AnchorMin;
        rect.anchorMax = info.AnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 선택된 가장자리만 Safe Area로 제한하고 나머지는 전체 화면 영역을 유지한다.
    private static Rect ApplyEdges(Rect safeArea, Rect fullArea, SafeAreaEdges edges)
    {
        float xMin = HasEdge(edges, SafeAreaEdges.Left) ? safeArea.xMin : fullArea.xMin;
        float xMax = HasEdge(edges, SafeAreaEdges.Right) ? safeArea.xMax : fullArea.xMax;
        float yMin = HasEdge(edges, SafeAreaEdges.Bottom) ? safeArea.yMin : fullArea.yMin;
        float yMax = HasEdge(edges, SafeAreaEdges.Top) ? safeArea.yMax : fullArea.yMax;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    // 플래그에 특정 가장자리 적용 값이 포함되어 있는지 확인한다.
    private static bool HasEdge(SafeAreaEdges edges, SafeAreaEdges edge)
    {
        return (edges & edge) == edge;
    }
}
