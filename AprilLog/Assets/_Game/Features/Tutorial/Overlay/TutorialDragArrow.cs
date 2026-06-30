// 두 타일을 잇는 점선과 화살표 머리. 시작점에서 머리까지 점이 차례로 드러났다 사라지길 반복한다.
// 좌표는 UI(오버레이 Canvas) 기준 — 보드가 월드 오브젝트면 변환을 따로 맞춰야 한다.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class TutorialDragArrow : MonoBehaviour
{
    [Header("스프라이트")]
    [SerializeField] private Sprite _dotSprite;        // 점(원형)
    [SerializeField] private Sprite _arrowHeadSprite;  // 화살표 머리

    [Header("점/머리 크기(px)")]
    [SerializeField] private float _dotSize = 14f;
    [SerializeField] private float _dotSpacing = 30f;   // 점 사이 간격(클수록 듬성)
    [SerializeField] private float _headSize = 48f;

    [Header("연출 시간(초)")]
    [SerializeField] private float _revealDuration = 1f;    // 시작→머리까지 드러나는 시간
    [SerializeField] private float _holdDuration = 0.15f;   // 다 보인 뒤 유지
    [SerializeField] private float _fadeDuration = 0.25f;   // 사라지는 시간

    private RectTransform _rt, _parentRt, _headRt;
    private Canvas _canvas;
    private CanvasGroup _group;
    private readonly List<RectTransform> _dots = new();

    private RectTransform _from, _to;
    private float _t;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        // The parent can be a small grouping rect, so use this full-screen arrow rect for coordinate conversion.
        _parentRt = _rt;
        _canvas = GetComponentInParent<Canvas>();
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;   // 입력 막지 않음

        _headRt = CreateImage("ArrowHead", _arrowHeadSprite);
        _headRt.pivot = new Vector2(0.5f, 0.5f);
        _headRt.sizeDelta = new Vector2(_headSize, _headSize);

        Hide();
    }

    // ---------- 외부 API ----------

    /// <summary>두 타일 사이에 점선 화살표를 띄우고 반복 재생한다.</summary>
    public void ShowDrag(RectTransform from, RectTransform to)
    {
        if (from == null || to == null) { Hide(); return; }
        gameObject.SetActive(true);
        _from = from; _to = to;
        _t = 0f;
    }

    /// <summary>화살표를 끈다.</summary>
    public void Hide()
    {
        _from = _to = null;
        gameObject.SetActive(false);
    }

    // ---------- 내부 ----------

    private void LateUpdate()
    {
        if (_from == null || _to == null || _parentRt == null) return;
        if (!TryLocal(_from, out Vector2 s) || !TryLocal(_to, out Vector2 e)) return;

        Vector2 delta = e - s;
        float fullLen = delta.magnitude;
        Vector2 dir = fullLen > 0.001f ? delta / fullLen : Vector2.right;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        // 시간 진행(일시정지 중에도 unscaled)
        float cycle = _revealDuration + _holdDuration + _fadeDuration;
        _t += Time.unscaledDeltaTime;
        if (cycle > 0f && _t >= cycle) _t -= cycle;

        float lenT, alpha;
        if (_t < _revealDuration)
        {
            lenT = _revealDuration <= 0f ? 1f : _t / _revealDuration;
            alpha = 1f;
        }
        else if (_t < _revealDuration + _holdDuration)
        {
            lenT = 1f; alpha = 1f;
        }
        else
        {
            lenT = 1f;
            float ft = (_t - _revealDuration - _holdDuration) / Mathf.Max(0.0001f, _fadeDuration);
            alpha = Mathf.Clamp01(1f - ft);
        }

        float curLen = fullLen * lenT;

        // 점 배치: 시작점에서 간격마다 하나씩, 현재 길이까지만
        int count = _dotSpacing > 0.01f ? Mathf.FloorToInt(curLen / _dotSpacing) : 0;
        for (int i = 0; i < count; i++)
        {
            RectTransform dot = GetDot(i);
            dot.gameObject.SetActive(true);
            dot.anchoredPosition = s + dir * ((i + 1) * _dotSpacing);
            dot.sizeDelta = new Vector2(_dotSize, _dotSize);
        }
        for (int i = count; i < _dots.Count; i++)
            _dots[i].gameObject.SetActive(false);

        // 머리: 현재 끝점에 위치, 방향각으로 회전
        _headRt.anchoredPosition = s + dir * curLen;
        _headRt.localEulerAngles = new Vector3(0f, 0f, angle);

        _group.alpha = alpha;
    }

    private RectTransform GetDot(int index)
    {
        while (_dots.Count <= index)
        {
            RectTransform dot = CreateImage("Dot_" + _dots.Count, _dotSprite);
            dot.pivot = new Vector2(0.5f, 0.5f);
            dot.SetAsFirstSibling();   // 점은 머리보다 뒤에 그려지게
            _dots.Add(dot);
        }
        return _dots[index];
    }

    private RectTransform CreateImage(string childName, Sprite sprite)
    {
        var go = new GameObject(childName, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(_rt, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;   // 좌하단(0,0) 기준
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return rt;
    }

    private bool TryLocal(RectTransform target, out Vector2 local)
    {
        local = default;
        Camera cam = GetEventCamera();
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRt, screen, cam, out Vector2 p))
            return false;
        Rect pr = _parentRt.rect;
        local = p - new Vector2(pr.xMin, pr.yMin);   // 좌하단(0,0) 기준
        return true;
    }

    private Camera GetEventCamera()
    {
        if (_canvas == null) return null;
        Canvas root = _canvas.rootCanvas;
        return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }
}
