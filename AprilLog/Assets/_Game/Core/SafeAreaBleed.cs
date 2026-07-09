// 안전영역 루트 아래에 있지만 화면 전체를 덮어야 하는 오브젝트(딤 등)를
// 부모 계층의 인셋과 무관하게 루트 캔버스 전체 크기로 펼친다.
// 프리팹 내부 어디에 중첩돼 있어도 동작한다.

using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaBleed : MonoBehaviour
{
    [Tooltip("화면 회전/해상도 변경 시 재적용을 받기 위한 SafeAreaFitter. 비워두면 부모에서 찾는다.")]
    [SerializeField] private SafeAreaFitter _fitter;

    private RectTransform _rect;
    private RectTransform _canvasRect;
    private readonly Vector3[] _corners = new Vector3[4];

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        CacheRefs();
    }

    private void CacheRefs()
    {
        if (_fitter == null)
            _fitter = GetComponentInParent<SafeAreaFitter>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            _canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        if (_canvasRect == null) CacheRefs();

        if (_fitter != null)
            _fitter.OnSafeAreaChanged += OnSafeAreaChanged;

        Apply();
    }

    private void OnDisable()
    {
        if (_fitter != null)
            _fitter.OnSafeAreaChanged -= OnSafeAreaChanged;
    }

    private void OnSafeAreaChanged(SafeAreaInfo info) => Apply();

    // 루트 캔버스의 월드 코너를 이 오브젝트의 부모 로컬 좌표로 변환해,
    // 부모가 어떤 크기든 화면 전체를 덮도록 offset을 맞춘다.
    private void Apply()
    {
        if (_rect == null || _canvasRect == null) return;

        RectTransform parent = _rect.parent as RectTransform;
        if (parent == null) return;

        _canvasRect.GetWorldCorners(_corners); // 0:좌하 1:좌상 2:우상 3:우하
        Vector2 bottomLeft = parent.InverseTransformPoint(_corners[0]);
        Vector2 topRight   = parent.InverseTransformPoint(_corners[2]);
        Rect pr = parent.rect;

        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = new Vector2(bottomLeft.x - pr.xMin, bottomLeft.y - pr.yMin);
        _rect.offsetMax = new Vector2(topRight.x - pr.xMax, topRight.y - pr.yMax);
    }
}
