// 대상을 가리키며 위아래로 왕복하는 손가락. 대상이 움직이면 따라가고, 일시정지 중에도 움직인다.

using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TutorialFingerGuide : MonoBehaviour
{
    [Header("대상 기준 위치 오프셋(px) — 보통 버튼 아래에서 위를 가리킴")]
    [SerializeField] private Vector2 _offset = new Vector2(0f, -40f);

    [Header("위아래 왕복")]
    [SerializeField] private float _bobAmplitude = 15f;   // 이동 폭(px)
    [SerializeField] private float _bobSpeed = 3f;         // 왕복 속도

    private RectTransform _rt;
    private RectTransform _parentRt;
    private Canvas _canvas;
    private RectTransform _target;
    private Vector2 _basePos;
    private float _time;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _parentRt = _rt.parent as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
        // 좌하단(0,0) 기준 → 부모 피벗에 무관하게 좌표 계산
        _rt.anchorMin = Vector2.zero;
        _rt.anchorMax = Vector2.zero;
        Hide();
    }

    // ---------- 외부 API ----------

    /// <summary>대상을 가리키며 왕복을 시작한다.</summary>
    public void PointAt(RectTransform target)
    {
        if (target == null) { Hide(); return; }
        gameObject.SetActive(true);
        _target = target;
        _time = 0f;
        UpdateBasePos();
    }

    /// <summary>손가락을 끈다.</summary>
    public void Hide()
    {
        _target = null;
        gameObject.SetActive(false);
    }

    // ---------- 내부 ----------

    private void LateUpdate()
    {
        if (_target == null) return;
        UpdateBasePos();
        _time += Time.unscaledDeltaTime * _bobSpeed;   // 일시정지 중에도 움직이게 unscaled
        float bob = Mathf.Sin(_time) * _bobAmplitude;
        _rt.anchoredPosition = _basePos + new Vector2(0f, bob);
    }

    private void UpdateBasePos()
    {
        if (_parentRt == null) return;
        Camera cam = GetEventCamera();

        // 대상 중심을 부모 로컬좌표로 변환
        var corners = new Vector3[4];
        _target.GetWorldCorners(corners);              // 0:좌하 2:우상
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRt, screen, cam, out Vector2 local))
            return;

        // 피벗 기준 좌표 → 좌하단(0,0) 기준으로 변환 후 오프셋 적용
        Rect pr = _parentRt.rect;
        _basePos = (local - new Vector2(pr.xMin, pr.yMin)) + _offset;
    }

    private Camera GetEventCamera()
    {
        if (_canvas == null) return null;
        Canvas root = _canvas.rootCanvas;
        return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }
}
