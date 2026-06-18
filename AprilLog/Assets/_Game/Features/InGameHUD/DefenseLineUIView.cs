//담당자: 조규민
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 방어선 표시 이미지를 월드 SpriteRenderer로 표시한다.
/// </summary>
public class DefenseLineUIView : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private Transform _defenseLine;
    [SerializeField] private SpriteRenderer _lineRenderer;
    [SerializeField] private Sprite _lineSprite;

    [Header("표시")]
    [Tooltip("월드 기준 방어선 표시 크기입니다.")]
    [SerializeField] private Vector2 _lineSize = new Vector2(12f, 1.5f);
    [SerializeField] private int _sortingOrder = 5;

    [Header("위치")]
    [SerializeField] private bool _followContinuously;
    [SerializeField] private float _yOffset;

    private void Awake()
    {
        DetachFromCanvas();
        EnsureRenderer();
        ApplyRendererSettings();
    }

    private void OnEnable()
    {
        UpdatePosition();
    }

    private void LateUpdate()
    {
        if (!_followContinuously)
        {
            return;
        }

        UpdatePosition();
    }

    public void UpdatePosition()
    {
        if (_defenseLine == null)
        {
            return;
        }

        transform.position = _defenseLine.position + Vector3.up * _yOffset;
    }

    private void DetachFromCanvas()
    {
        if (GetComponentInParent<Canvas>() == null)
        {
            return;
        }

        transform.SetParent(null, false);
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void EnsureRenderer()
    {
        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (_lineSprite == null)
        {
            _lineSprite = GetComponentInChildren<Image>(true)?.sprite;
        }

        if (_lineRenderer == null)
        {
            _lineRenderer = CreateLineRenderer();
        }

        DisableLegacyGraphics();
    }

    private SpriteRenderer CreateLineRenderer()
    {
        var line = transform.Find("Line");
        if (line == null)
        {
            line = new GameObject("Line").transform;
            line.SetParent(transform, false);
        }

        return line.gameObject.AddComponent<SpriteRenderer>();
    }

    private void ApplyRendererSettings()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        _lineRenderer.sprite = _lineSprite;
        _lineRenderer.sortingOrder = _sortingOrder;
        _lineRenderer.drawMode = SpriteDrawMode.Sliced;
        _lineRenderer.size = _lineSize;
        _lineRenderer.transform.localPosition = Vector3.zero;
        _lineRenderer.transform.localRotation = Quaternion.identity;
        _lineRenderer.transform.localScale = Vector3.one;
    }

    private void DisableLegacyGraphics()
    {
        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in graphics)
        {
            graphic.enabled = false;
        }
    }
}
