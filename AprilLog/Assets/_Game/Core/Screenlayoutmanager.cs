// 담당자 : 정승우
// 설명   : 화면 비율 관리 -- 세로 화면에서 디펜스/퍼즐 영역 비율 유지

using UnityEngine;

public class ScreenLayoutManager : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("영역 비율")]
    [Tooltip("디펜스 영역이 전체 화면에서 차지하는 비율 (0~1)")]
    [SerializeField] private float _defenseRatio = 0.55f;

    [Tooltip("캐릭터/조합식 경계선 높이 (고정 pixel)")]
    [SerializeField] private float _middleBarHeight = 80f;

    [Header("참조")]
    [SerializeField] private RectTransform _defenseArea;
    [SerializeField] private RectTransform _puzzleArea;
    [SerializeField] private RectTransform _middleBar;

    private float _lastScreenHeight;

    private void Start()
    {
        ApplyLayout();
    }

    private void Update()
    {
        // 화면 크기 바뀌면 재계산 (폴더블 기기 대응)
        if (Mathf.Abs(Screen.height - _lastScreenHeight) > 1f)
            ApplyLayout();
    }

    private void ApplyLayout()
    {
        _lastScreenHeight = Screen.height;

        if (_defenseArea == null || _puzzleArea == null) return;

        // 전체 높이에서 경계선 빼고 남은 걸 비율로 나눔
        // SafeArea 안에서의 비율이라 SafeAreaFitter 아래에 있어야 정확함

        float totalHeight = _defenseArea.parent != null
            ? ((RectTransform)_defenseArea.parent).rect.height
            : Screen.height;

        float middleH = _middleBar != null ? _middleBarHeight : 0f;
        float usableHeight = totalHeight - middleH;

        float defenseH = usableHeight * _defenseRatio;
        float puzzleH = usableHeight * (1f - _defenseRatio);

        // 디펜스: 상단에 붙이기
        _defenseArea.anchorMin = new Vector2(0f, 1f - (defenseH / totalHeight));
        _defenseArea.anchorMax = new Vector2(1f, 1f);
        _defenseArea.offsetMin = Vector2.zero;
        _defenseArea.offsetMax = Vector2.zero;

        // 퍼즐: 하단에 붙이기
        _puzzleArea.anchorMin = new Vector2(0f, 0f);
        _puzzleArea.anchorMax = new Vector2(1f, puzzleH / totalHeight);
        _puzzleArea.offsetMin = Vector2.zero;
        _puzzleArea.offsetMax = Vector2.zero;

        // 경계선: 가운데
        if (_middleBar != null)
        {
            float middleBottom = puzzleH / totalHeight;
            float middleTop = middleBottom + (middleH / totalHeight);
            _middleBar.anchorMin = new Vector2(0f, middleBottom);
            _middleBar.anchorMax = new Vector2(1f, middleTop);
            _middleBar.offsetMin = Vector2.zero;
            _middleBar.offsetMax = Vector2.zero;
        }
    }
}