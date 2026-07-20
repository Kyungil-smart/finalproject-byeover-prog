// 담당자 : 정승우
// 설명   : 화면 비율 관리 -- 세로 화면에서 디펜스/퍼즐 영역 비율 유지

// 2차 수정자 : 조규민
// 수정 내용 : 퍼즐 영역 하단 확장값을 Inspector에서 조절해 하단 SafeArea 바깥 배경 노출을 가릴 수 있도록 옵션 추가
//           MiddleBar Y 오프셋을 Inspector에서 조절해 경계선 이미지만 이동할 수 있도록 옵션 추가

using UnityEngine;

public class ScreenLayoutManager : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("영역 비율")]
    [Tooltip("디펜스 영역이 전체 화면에서 차지하는 비율 (0~1)")]
    [SerializeField] private float _defenseRatio = 0.55f;

    [Tooltip("캐릭터/조합식 경계선 높이 (고정 pixel)")]
    [SerializeField] private float _middleBarHeight = 80f;

    [Tooltip("캐릭터/조합식 경계선을 위아래로 이동하는 값(pixel). 양수 = 위")]
    [SerializeField] private float _middleBarYOffset;

    [Header("참조")]
    [SerializeField] private RectTransform _defenseArea;
    [SerializeField] private RectTransform _puzzleArea;
    // 수정자: 조규민 - TableCanvas가 SafeArea 안에 유지되도록 배경 전용 하단 확장 대상을 분리한다.
    [SerializeField] private RectTransform _puzzleOverscanBackground;
    [SerializeField] private RectTransform _middleBar;

    [Tooltip("퍼즐 영역을 SafeArea 아래로 추가 확장하는 높이(pixel). 하단 배경 노출을 가릴 때 사용")]
    [SerializeField] private float _puzzleBottomOverscan = 120f;

    [Tooltip("퍼즐 영역 하단에 비워둘 높이(pixel). 화면 바닥 고정 경험치바 등이 카드에 가려지지 않게 예약. 0 = 기존 동작(하단 오버스캔)")]
    [SerializeField] private float _puzzleBottomReservedHeight;

    private float _lastScreenHeight;
    private float _lastLayoutHeight;
    private float _lastBottomUnsafeHeight;
    private float _lastPuzzleBottomOverscan;
    private float _lastMiddleBarYOffset;
    private float _lastPuzzleBottomReserved;

    private void Start()
    {
        ApplyLayout();
    }

    private void Update()
    {
        // 화면 크기 바뀌면 재계산 (폴더블 기기 대응)
        float currentLayoutHeight = GetTotalHeight();
        float currentBottomUnsafeHeight = GetBottomUnsafeHeight(currentLayoutHeight);
        if (Mathf.Abs(Screen.height - _lastScreenHeight) > 1f ||
            Mathf.Abs(currentLayoutHeight - _lastLayoutHeight) > 1f ||
            Mathf.Abs(currentBottomUnsafeHeight - _lastBottomUnsafeHeight) > 1f ||
            !Mathf.Approximately(_puzzleBottomOverscan, _lastPuzzleBottomOverscan) ||
            !Mathf.Approximately(_puzzleBottomReservedHeight, _lastPuzzleBottomReserved) ||
            !Mathf.Approximately(_middleBarYOffset, _lastMiddleBarYOffset))
        {
            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        _lastScreenHeight = Screen.height;

        if (_defenseArea == null || _puzzleArea == null) return;

        // 전체 높이에서 경계선 빼고 남은 걸 비율로 나눔
        // SafeArea 안에서의 비율이라 SafeAreaFitter 아래에 있어야 정확함

        float totalHeight = GetTotalHeight();
        if (totalHeight <= 0f)
        {
            return;
        }

        _lastLayoutHeight = totalHeight;
        _lastBottomUnsafeHeight = GetBottomUnsafeHeight(totalHeight);
        _lastPuzzleBottomOverscan = _puzzleBottomOverscan;
        _lastMiddleBarYOffset = _middleBarYOffset;
        _lastPuzzleBottomReserved = _puzzleBottomReservedHeight;

        float middleH = _middleBar != null ? _middleBarHeight : 0f;
        float usableHeight = Mathf.Max(0f, totalHeight - middleH);

        float defenseH = usableHeight * _defenseRatio;
        float puzzleH = usableHeight * (1f - _defenseRatio);
        float maxMiddleBottomPixel = Mathf.Max(0f, totalHeight - middleH);
        float middleBottomPixel = Mathf.Clamp(puzzleH, 0f, maxMiddleBottomPixel);
        float middleTopPixel = middleBottomPixel + middleH;
        float middleBottomRatio = middleBottomPixel / totalHeight;
        float middleTopRatio = middleTopPixel / totalHeight;

        // 디펜스: 상단에 붙이기
        _defenseArea.anchorMin = new Vector2(0f, 1f - (defenseH / totalHeight));
        _defenseArea.anchorMax = new Vector2(1f, 1f);
        _defenseArea.offsetMin = Vector2.zero;
        _defenseArea.offsetMax = Vector2.zero;

        // 퍼즐: 하단에 붙이기
        // 추가: 조규민 - 하단 SafeArea 바깥으로 퍼즐 영역을 확장해 MainCanvas 배경 노출을 막는다.
        float puzzleBottomOverscanRatio = GetPuzzleBottomOverscanRatio(totalHeight);

        // 하단 예약 높이가 있으면 퍼즐 콘텐츠 바닥을 화면 바닥에서 그만큼 위로 올린다(경험치바 자리 확보).
        // 0이면 기존처럼 하단 SafeArea 바깥까지 확장(조규민).
        float puzzleContentBottomRatio = _puzzleBottomReservedHeight > 0f
            ? Mathf.Clamp01(_puzzleBottomReservedHeight / totalHeight)
            : -puzzleBottomOverscanRatio;

        _puzzleArea.anchorMin = new Vector2(0f, puzzleContentBottomRatio);
        _puzzleArea.anchorMax = new Vector2(1f, middleBottomRatio);
        _puzzleArea.offsetMin = Vector2.zero;
        _puzzleArea.offsetMax = Vector2.zero;

        if (_puzzleOverscanBackground != null)
        {
            _puzzleOverscanBackground.anchorMin = new Vector2(0f, -puzzleBottomOverscanRatio);
            _puzzleOverscanBackground.anchorMax = new Vector2(1f, middleBottomRatio);
            _puzzleOverscanBackground.offsetMin = Vector2.zero;
            _puzzleOverscanBackground.offsetMax = Vector2.zero;
        }

        // 경계선: 가운데
        if (_middleBar != null)
        {
            float offsetMiddleBottomPixel = Mathf.Clamp(middleBottomPixel + _middleBarYOffset, 0f, maxMiddleBottomPixel);
            float offsetMiddleTopPixel = offsetMiddleBottomPixel + middleH;
            float offsetMiddleBottomRatio = offsetMiddleBottomPixel / totalHeight;
            float offsetMiddleTopRatio = offsetMiddleTopPixel / totalHeight;
            _middleBar.anchorMin = new Vector2(0f, offsetMiddleBottomRatio);
            _middleBar.anchorMax = new Vector2(1f, offsetMiddleTopRatio);
            _middleBar.offsetMin = Vector2.zero;
            _middleBar.offsetMax = Vector2.zero;
        }
    }

    private float GetPuzzleBottomOverscanRatio(float totalHeight)
    {
        if (totalHeight <= 0f)
        {
            return 0f;
        }

        float overscan = Mathf.Max(Mathf.Max(0f, _puzzleBottomOverscan), GetBottomUnsafeHeight(totalHeight));
        return Mathf.Clamp01(overscan / totalHeight);
    }

    private float GetBottomUnsafeHeight(float totalHeight)
    {
        if (totalHeight <= 0f || Screen.height <= 0)
        {
            return 0f;
        }

        Rect safeArea = Screen.safeArea;
        if (safeArea.height <= 0f)
        {
            return 0f;
        }

        // 수정자: 조규민 - 기기별 하단 unsafe 영역을 SafeAreaRoot 로컬 높이 기준으로 환산해 배경 확장량에 반영한다.
        return totalHeight * Mathf.Max(0f, safeArea.yMin) / safeArea.height;
    }

    private float GetTotalHeight()
    {
        if (_defenseArea != null && _defenseArea.parent is RectTransform parentRect)
        {
            return parentRect.rect.height;
        }

        return Screen.height;
    }
}
