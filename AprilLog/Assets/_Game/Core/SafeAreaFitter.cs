// 담당자 : 정승우
// 설명   : Safe Area 자동 맞춤 - 노치/펀치홀 대응
// 수정자 : 정승우
// 수정내용 : Safe Area 계산 API, 적용 방향 옵션, 변경 이벤트, 에디터 테스트 옵션 추가

using System;
using UnityEngine;

/// <summary>
/// Canvas 바로 아래 SafeAreaRoot에 붙여 화면 안전 영역 안으로 UI를 배치한다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    public event Action<SafeAreaInfo> OnSafeAreaChanged;

    // ---------- 설정 ----------
    [Header("적용 방향")]
    [Tooltip("Safe Area를 적용할 방향")]
    [SerializeField] private SafeAreaEdges _appliedEdges = SafeAreaEdges.All;

    [Tooltip("활성화될 때 즉시 Safe Area를 적용할지 여부")]
    [SerializeField] private bool _applyOnEnable = true;

#if UNITY_EDITOR
    [Header("에디터 테스트")]
    [Tooltip("에디터에서 실제 기기 Safe Area 대신 테스트 값을 사용할지 여부")]
    [SerializeField] private bool _useEditorOverride;

    [Tooltip("에디터 테스트용 Safe Area 픽셀 Rect")]
    [SerializeField] private Rect _editorSafeAreaOverride = new Rect(0f, 80f, 1080f, 2200f);

    [Tooltip("에디터 테스트용 화면 크기")]
    [SerializeField] private Vector2Int _editorScreenSizeOverride = new Vector2Int(1080, 2280);
#endif

    // ---------- 상태 ----------
    private RectTransform _rect;
    private SafeAreaInfo _currentInfo;
    private Rect _lastSafeArea;
    private int _lastScreenWidth;
    private int _lastScreenHeight;
    private SafeAreaEdges _lastAppliedEdges;
    private bool _hasApplied;

    public SafeAreaInfo CurrentInfo => _currentInfo;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (_applyOnEnable)
            Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    public void SetAppliedEdges(SafeAreaEdges edges)
    {
        if (_appliedEdges == edges) return;

        _appliedEdges = edges;
        Refresh(true);
    }

    public void Refresh(bool force)
    {
        Rect safeArea = GetSafeArea();
        int screenWidth = GetScreenWidth();
        int screenHeight = GetScreenHeight();

        if (!force && !HasChanged(safeArea, screenWidth, screenHeight))
            return;

        _lastSafeArea = safeArea;
        _lastScreenWidth = screenWidth;
        _lastScreenHeight = screenHeight;
        _lastAppliedEdges = _appliedEdges;
        _hasApplied = true;

        _currentInfo = SafeAreaUtility.CreateInfo(safeArea, screenWidth, screenHeight, _appliedEdges);
        SafeAreaUtility.ApplyTo(_rect, _currentInfo);

        OnSafeAreaChanged?.Invoke(_currentInfo);
    }

    private bool HasChanged(Rect safeArea, int screenWidth, int screenHeight)
    {
        if (!_hasApplied) return true;
        if (_lastAppliedEdges != _appliedEdges) return true;
        if (_lastScreenWidth != screenWidth) return true;
        if (_lastScreenHeight != screenHeight) return true;
        return _lastSafeArea != safeArea;
    }

    private Rect GetSafeArea()
    {
#if UNITY_EDITOR
        if (_useEditorOverride)
            return _editorSafeAreaOverride;
#endif
        return Screen.safeArea;
    }

    private int GetScreenWidth()
    {
#if UNITY_EDITOR
        if (_useEditorOverride)
            return Mathf.Max(1, _editorScreenSizeOverride.x);
#endif
        return Screen.width;
    }

    private int GetScreenHeight()
    {
#if UNITY_EDITOR
        if (_useEditorOverride)
            return Mathf.Max(1, _editorScreenSizeOverride.y);
#endif
        return Screen.height;
    }
}
