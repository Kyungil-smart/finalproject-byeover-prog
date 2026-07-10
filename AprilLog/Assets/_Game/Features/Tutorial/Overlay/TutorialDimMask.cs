// 화면을 어둡게 덮되 지정한 영역들만 구멍으로 비워 강조한다.
// 여러 구멍을 지원한다(밴드 분할로 구멍을 피한 사각형들로 화면을 덮음). 구멍은 그래픽이 없어 클릭이 통과한다.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class TutorialDimMask : MonoBehaviour
{
    [Header("딤 색/투명도")]
    [SerializeField] private Color _dimColor = new Color(0f, 0f, 0f, 0.5f);

    [Header("구멍 여백")]
    [SerializeField] private float _holePadding = 8f;

    private RectTransform _rt;
    private Canvas _canvas;
    private readonly List<RectTransform> _panels = new();
    private RectTransform[] _currentTargets;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        Hide();
    }

    // ---------- 외부 API ----------

    /// <summary>대상 영역만 구멍으로 비우고 나머지를 딤한다.</summary>
    public void ShowWithHole(RectTransform target)
    {
        if (target == null) { ShowFull(); return; }
        ShowWithHoles(new[] { target });
    }

    /// <summary>여러 대상을 각각 구멍으로 비우고 나머지를 딤한다.</summary>
    public void ShowWithHoles(RectTransform[] targets)
    {
        if (targets == null || targets.Length == 0) { ShowFull(); return; }
        gameObject.SetActive(true);
        _currentTargets = targets;
        UpdateHoles();
    }

    /// <summary>구멍 없이 화면 전체를 딤한다(대상 강조 없는 단계). 대상 추적을 끊는다.</summary>
    public void ShowFull()
    {
        _currentTargets = null;
        CoverFull();
    }

    // 대상 추적은 유지한 채 화면 전체를 덮는다.
    // (대상이 아직 화면 밖 = 레이아웃/스크롤 로딩 중일 때 사용. 다음 프레임 LateUpdate에서 대상이
    //  화면에 들어오면 구멍을 다시 뚫는다. ShowFull처럼 _currentTargets를 지우면 영구히 전체 딤에 갇힌다.)
    private void CoverFull()
    {
        gameObject.SetActive(true);
        Rect full = _rt.rect;
        SetPanel(0, full.xMin, full.yMin, full.xMax, full.yMax);
        DisableFrom(1);
    }

    /// <summary>딤을 끈다.</summary>
    public void Hide()
    {
        _currentTargets = null;
        gameObject.SetActive(false);
    }

    // ---------- 내부 ----------

    // 대상이 스크롤/애니메이션으로 움직일 수 있으니 매 프레임 추적
    private void LateUpdate()
    {
        if (_currentTargets != null && _currentTargets.Length > 0)
            UpdateHoles();
    }

    // 구멍들을 피해 화면을 덮는 사각형들을 만든다(밴드 분할).
    private void UpdateHoles()
    {
        Camera cam = GetEventCamera();
        Rect full = _rt.rect;

        // 대상들의 로컬 Rect(여백 적용, 화면 안으로 클램프) 수집
        var holes = new List<Rect>();
        foreach (RectTransform t in _currentTargets)
        {
            if (t == null || !TryGetLocalRect(t, cam, out Rect r)) continue;
            r.xMin -= _holePadding; r.yMin -= _holePadding;
            r.xMax += _holePadding; r.yMax += _holePadding;
            float xMin = Mathf.Max(r.xMin, full.xMin), xMax = Mathf.Min(r.xMax, full.xMax);
            float yMin = Mathf.Max(r.yMin, full.yMin), yMax = Mathf.Min(r.yMax, full.yMax);
            if (xMax > xMin && yMax > yMin) holes.Add(Rect.MinMaxRect(xMin, yMin, xMax, yMax));
        }
        // 대상이 화면 밖이라 구멍이 없으면 전체를 덮되, 추적은 유지해 대상이 화면에 들어오면 다시 뚫는다.
        if (holes.Count == 0) { CoverFull(); return; }

        // Y 경계마다 가로 밴드로 자르고, 각 밴드에서 구멍의 X 구간을 피해 덮는다.
        var ys = new List<float> { full.yMin, full.yMax };
        foreach (Rect h in holes) { ys.Add(h.yMin); ys.Add(h.yMax); }
        ys.Sort();

        int panel = 0;
        for (int i = 0; i < ys.Count - 1; i++)
        {
            float y0 = ys[i], y1 = ys[i + 1];
            if (y1 - y0 <= 0.5f) continue;
            float midY = (y0 + y1) * 0.5f;

            // 이 밴드에 걸리는 구멍들의 X 구간 수집 후 병합
            var segs = new List<Vector2>();
            foreach (Rect h in holes)
                if (h.yMin <= midY && h.yMax >= midY) segs.Add(new Vector2(h.xMin, h.xMax));
            segs.Sort((a, b) => a.x.CompareTo(b.x));

            float x = full.xMin;
            foreach (Vector2 seg in segs)
            {
                if (seg.x > x) SetPanel(panel++, x, y0, seg.x, y1);
                x = Mathf.Max(x, seg.y);
            }
            if (x < full.xMax) SetPanel(panel++, x, y0, full.xMax, y1);
        }
        DisableFrom(panel);
    }

    private RectTransform GetPanel(int index)
    {
        while (_panels.Count <= index)
        {
            var go = new GameObject("Dim_" + _panels.Count, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_rt, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = _dimColor;
            img.raycastTarget = true;   // 딤 영역은 입력 차단
            _panels.Add(rt);
        }
        return _panels[index];
    }

    private void DisableFrom(int index)
    {
        for (int i = index; i < _panels.Count; i++)
            _panels[i].gameObject.SetActive(false);
    }

    // 컨테이너 로컬좌표(xMin,yMin,xMax,yMax)로 패널 배치
    private void SetPanel(int index, float xMin, float yMin, float xMax, float yMax)
    {
        RectTransform panel = GetPanel(index);
        panel.gameObject.SetActive(true);
        Rect full = _rt.rect;
        panel.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        panel.anchoredPosition = new Vector2(xMin - full.xMin, yMin - full.yMin);
    }

    private Camera GetEventCamera()
    {
        if (_canvas == null) return null;
        Canvas root = _canvas.rootCanvas;
        return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }

    // 대상 RectTransform 을 이 딤 컨테이너의 로컬 Rect로 변환
    // dimCam: 딤(_rt) 캔버스 기준 카메라. 대상은 딤과 다른 렌더모드/카메라의 캔버스일 수 있어
    // 스크린 좌표는 대상 자신의 캔버스 카메라로 구한다(안 그러면 변환 실패로 구멍이 안 뚫린다).
    private bool TryGetLocalRect(RectTransform target, Camera dimCam, out Rect rect)
    {
        rect = default;
        Camera targetCam = GetCameraFor(target);
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);    // 0:좌하 1:좌상 2:우상 3:우하

        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(targetCam, corners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, screen, dimCam, out Vector2 local))
                return false;
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        rect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        return true;
    }

    // 대상을 실제로 렌더하는 캔버스의 카메라(Overlay면 null). WorldToScreenPoint에 사용.
    private static Camera GetCameraFor(RectTransform target)
    {
        Canvas canvas = target != null ? target.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return null;
        Canvas root = canvas.rootCanvas;
        if (root.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return root.worldCamera != null ? root.worldCamera : Camera.main;
    }

#if UNITY_EDITOR
    // 인스펙터에서 색을 바꾸면 런타임 패널에도 반영
    private void OnValidate()
    {
        foreach (RectTransform p in _panels)
        {
            if (p == null) continue;
            var img = p.GetComponent<Image>();
            if (img != null) img.color = _dimColor;
        }
    }
#endif
}
