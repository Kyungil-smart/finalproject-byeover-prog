// 담당자 : 정승우
// 설명   : Safe Area 자동 맞춤 - 노치/펀치홀 대응

using UnityEngine;

/// <summary>
/// Canvas 바로 아래 Panel에 붙이면 Safe Area에 맞춰 자동 리사이즈된다.
/// 노치, 펀치홀, 네비게이션 바 영역을 피해서 UI를 배치함.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rect;
    private Rect _lastSafeArea;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        // 기기 회전이나 소프트키 변경으로 Safe Area가 바뀔 수 있어서 매 프레임 체크
        if (Screen.safeArea == _lastSafeArea) return;
        ApplySafeArea(Screen.safeArea);
    }

    private void ApplySafeArea(Rect safeArea)
    {
        _lastSafeArea = safeArea;

        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rect.anchorMin = anchorMin;
        _rect.anchorMax = anchorMax;
    }
}
