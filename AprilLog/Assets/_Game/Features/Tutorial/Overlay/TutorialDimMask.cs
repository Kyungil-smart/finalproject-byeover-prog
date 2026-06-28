// 화면을 어둡게 덮되 대상 영역만 비워 강조한다(상/하/좌/우 4분할 패널).
// 비워둔 구멍은 그래픽이 없어 클릭이 통과하고, 나머지 패널이 입력을 막는다.

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
    private RectTransform _topRt, _bottomRt, _leftRt, _rightRt;
    private Image _topImg, _bottomImg, _leftImg, _rightImg;
    private RectTransform _currentTarget;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        BuildPanels();
        Hide();
    }

    // ---------- 외부 API ----------

    /// <summary>대상 영역만 구멍으로 비우고 나머지를 딤한다.</summary>
    public void ShowWithHole(RectTransform target)
    {
        if (target == null) { ShowFull(); return; }
        gameObject.SetActive(true);
        _currentTarget = target;
        UpdateHole();
    }

    /// <summary>구멍 없이 화면 전체를 딤한다(대상 강조 없는 단계).</summary>
    public void ShowFull()
    {
        gameObject.SetActive(true);
        _currentTarget = null;
        Rect full = _rt.rect;
        SetPanel(_topRt, full.xMin, full.yMin, full.xMax, full.yMax);
        SetPanel(_bottomRt, 0, 0, 0, 0);
        SetPanel(_leftRt, 0, 0, 0, 0);
        SetPanel(_rightRt, 0, 0, 0, 0);
    }

    /// <summary>딤을 끈다.</summary>
    public void Hide()
    {
        _currentTarget = null;
        gameObject.SetActive(false);
    }

    // ---------- 내부 ----------

    // 대상이 스크롤/애니메이션으로 움직일 수 있으니 매 프레임 추적
    private void LateUpdate()
    {
        if (_currentTarget != null)
            UpdateHole();
    }

    private void BuildPanels()
    {
        _topImg = EnsurePanel(null, "Dim_Top", out _topRt);
        _bottomImg = EnsurePanel(null, "Dim_Bottom", out _bottomRt);
        _leftImg = EnsurePanel(null, "Dim_Left", out _leftRt);
        _rightImg = EnsurePanel(null, "Dim_Right", out _rightRt);
    }

    // 인스펙터에 꽂힌 패널이 있으면 그걸 쓰고, 없으면 새로 만든다. 어느 쪽이든 설정을 강제한다.
    private Image EnsurePanel(RectTransform assigned, string panelName, out RectTransform rt)
    {
        if (assigned != null)
        {
            rt = assigned;
        }
        else
        {
            var go = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            rt = (RectTransform)go.transform;
            rt.SetParent(_rt, false);
        }

        // 피벗/앵커를 컨테이너 좌하단(0,0) 기준으로 고정 → 피벗에 무관하게 좌표 계산
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;

        var img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.color = _dimColor;
        img.raycastTarget = true;   // 딤 영역은 입력 차단
        return img;
    }

    private void UpdateHole()
    {
        Camera cam = GetEventCamera();
        if (!TryGetLocalRect(_currentTarget, cam, out Rect hole)) { ShowFull(); return; }

        // 여백 적용
        hole.xMin -= _holePadding; hole.yMin -= _holePadding;
        hole.xMax += _holePadding; hole.yMax += _holePadding;

        Rect full = _rt.rect;
        // 구멍을 피해 4분할: 위/아래는 전체 폭, 좌/우는 구멍 높이만큼만 → 모서리 중복 딤 방지
        SetPanel(_topRt,    full.xMin, hole.yMax, full.xMax, full.yMax);
        SetPanel(_bottomRt, full.xMin, full.yMin, full.xMax, hole.yMin);
        SetPanel(_leftRt,   full.xMin, hole.yMin, hole.xMin, hole.yMax);
        SetPanel(_rightRt,  hole.xMax, hole.yMin, full.xMax, hole.yMax);
    }

    private Camera GetEventCamera()
    {
        if (_canvas == null) return null;
        Canvas root = _canvas.rootCanvas;
        return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }

    // 대상 RectTransform 을 이 딤 컨테이너의 로컬 Rect(피벗 기준)로 변환
    private bool TryGetLocalRect(RectTransform target, Camera cam, out Rect rect)
    {
        rect = default;
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);    // 0:좌하 1:좌상 2:우상 3:우하

        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, screen, cam, out Vector2 local))
                return false;
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        rect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        return true;
    }

    // 컨테이너 로컬좌표(xMin,yMin,xMax,yMax)로 패널 배치, 음수 크기는 0 처리
    private void SetPanel(RectTransform panel, float xMin, float yMin, float xMax, float yMax)
    {
        Rect full = _rt.rect;
        float w = Mathf.Max(0f, xMax - xMin);
        float h = Mathf.Max(0f, yMax - yMin);
        panel.sizeDelta = new Vector2(w, h);
        // 앵커=피벗=(0,0) 이므로 좌하단을 컨테이너 좌하단 기준 상대좌표로 지정
        panel.anchoredPosition = new Vector2(xMin - full.xMin, yMin - full.yMin);
    }

#if UNITY_EDITOR
    // 인스펙터에서 색을 바꾸면 런타임 패널에도 반영
    private void OnValidate()
    {
        if (_topImg == null) return;
        _topImg.color = _bottomImg.color = _leftImg.color = _rightImg.color = _dimColor;
    }
#endif
}
