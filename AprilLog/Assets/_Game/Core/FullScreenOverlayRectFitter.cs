//담당자: 조규민

using UnityEngine;

/// <summary>
/// SafeArea 하위에 있는 배경 오버레이를 최상위 Canvas 전체 영역까지 확장한다.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FullScreenOverlayRectFitter : MonoBehaviour
{
    [Header("화면 덮기 설정")]
    [Tooltip("비워두면 이 오브젝트의 RectTransform을 보정합니다.")]
    [SerializeField] private RectTransform _targetRect;

    [Tooltip("비워두면 최상위 Canvas의 RectTransform을 전체 화면 기준으로 사용합니다.")]
    [SerializeField] private RectTransform _coverageRoot;

    private Canvas _cachedRootCanvas;
    private RectTransform _cachedParent;
    private readonly Vector3[] _coverageCorners = new Vector3[4];
    private Vector2 _lastMin;
    private Vector2 _lastMax;
    private bool _hasLastBounds;

    private void OnEnable()
    {
        CacheReferences();
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
        Apply();
    }
#endif

    private void CacheReferences()
    {
        if (_targetRect == null)
        {
            _targetRect = GetComponent<RectTransform>();
        }

        _cachedParent = _targetRect != null
            ? _targetRect.parent as RectTransform
            : null;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        _cachedRootCanvas = parentCanvas != null
            ? parentCanvas.rootCanvas
            : null;
    }

    private void Apply()
    {
        if (_targetRect == null || _cachedParent == null)
        {
            CacheReferences();
        }

        if (_targetRect == null || _cachedParent == null)
        {
            return;
        }

        RectTransform coverageRoot = ResolveCoverageRoot();
        if (coverageRoot == null)
        {
            return;
        }

        CalculateCoverageBounds(coverageRoot, out Vector2 min, out Vector2 max);

        if (_hasLastBounds && _lastMin == min && _lastMax == max)
        {
            return;
        }

        _hasLastBounds = true;
        _lastMin = min;
        _lastMax = max;

        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min;
        Vector2 anchor = new Vector2(0.5f, 0.5f);
        Vector2 parentAnchorPosition = GetParentAnchorPosition(anchor);

        _targetRect.anchorMin = anchor;
        _targetRect.anchorMax = anchor;
        _targetRect.pivot = new Vector2(0.5f, 0.5f);
        _targetRect.anchoredPosition = center - parentAnchorPosition;
        _targetRect.sizeDelta = size;
    }

    private RectTransform ResolveCoverageRoot()
    {
        if (_coverageRoot != null)
        {
            return _coverageRoot;
        }

        if (_cachedRootCanvas == null)
        {
            CacheReferences();
        }

        return _cachedRootCanvas != null
            ? _cachedRootCanvas.transform as RectTransform
            : null;
    }

    private void CalculateCoverageBounds(RectTransform coverageRoot, out Vector2 min, out Vector2 max)
    {
        coverageRoot.GetWorldCorners(_coverageCorners);

        min = _cachedParent.InverseTransformPoint(_coverageCorners[0]);
        max = min;

        for (int i = 1; i < _coverageCorners.Length; i++)
        {
            Vector2 localPoint = _cachedParent.InverseTransformPoint(_coverageCorners[i]);
            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }
    }

    private Vector2 GetParentAnchorPosition(Vector2 anchor)
    {
        Rect parentRect = _cachedParent.rect;
        return new Vector2(
            (anchor.x - _cachedParent.pivot.x) * parentRect.width,
            (anchor.y - _cachedParent.pivot.y) * parentRect.height);
    }
}
