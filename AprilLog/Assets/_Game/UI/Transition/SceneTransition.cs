using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 씬 전환용 대각선 타일 와이프 오버레이.
// 화면을 정사각형 타일 그리드로 덮은 뒤(대각선 계단식 페이드) 씬을 비동기 로드하고 다시 걷어낸다.
// 씬에 미리 배치할 필요 없음 - 처음 호출 시 스스로 생성되어 씬 전환 사이에 유지된다.
public class SceneTransition : MonoBehaviour
{
    [Header("그리드")]
    [Tooltip("가로 타일 개수")]
    [SerializeField] private int _columns = 8;
    [Tooltip("세로 타일 개수")]
    [SerializeField] private int _rows = 11;
    [Tooltip("타일 색")]
    [SerializeField] private Color _tileColor = new Color(0.93f, 0.93f, 0.93f, 1f);

    [Header("연출")]
    [Tooltip("타일 한 장이 페이드되는 시간")]
    [SerializeField] private float _tileFadeDuration = 0.18f;
    [Tooltip("대각선 한 칸당 시작 딜레이(계단 간격)")]
    [SerializeField] private float _diagonalStagger = 0.035f;
    [Tooltip("덮은 뒤 걷어내기 전 대기 시간")]
    [SerializeField] private float _holdBetween = 0.05f;
    [Tooltip("타일 페이드 이징")]
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [Tooltip("와이프 시작 코너")]
    [SerializeField] private Corner _origin = Corner.TopLeft;

    public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

    private static SceneTransition _instance;
    private Image[,] _tiles;
    private bool _isPlaying;

    /// <summary>필요 시 오버레이를 생성하고 반환하는 싱글턴 접근자.</summary>
    public static SceneTransition Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[SceneTransition]");
                _instance = go.AddComponent<SceneTransition>();
            }
            return _instance;
        }
    }

    /// <summary>지정 씬을 타일 와이프 연출과 함께 로드한다.</summary>
    public static void Load(string sceneName) => Instance.LoadInternal(sceneName);

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        SetAllVisible(false);
    }

    private void LoadInternal(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneTransition] 씬 이름이 비어있습니다.", this);
            return;
        }
        if (_isPlaying) return;
        StartCoroutine(PlayAndLoad(sceneName));
    }

    private IEnumerator PlayAndLoad(string sceneName)
    {
        _isPlaying = true;

        // 덮기
        yield return Wipe(true);
        yield return new WaitForSecondsRealtime(_holdBetween);

        // 화면이 덮인 상태에서 로드
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;

        // 걷어내기
        yield return Wipe(false);

        _isPlaying = false;
    }

    // cover=true: 투명 -> 불투명으로 덮기 / false: 불투명 -> 투명으로 걷어내기
    private IEnumerator Wipe(bool cover)
    {
        float from = cover ? 0f : 1f;
        float to = cover ? 1f : 0f;
        float totalWait = 0f;

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                var img = _tiles[x, y];
                var c = _tileColor;
                c.a = from;
                img.color = c;

                float delay = DiagonalStep(x, y) * _diagonalStagger;
                img.DOFade(to, _tileFadeDuration).SetDelay(delay).SetEase(_ease).SetUpdate(true);

                totalWait = Mathf.Max(totalWait, delay + _tileFadeDuration);
            }
        }

        yield return new WaitForSecondsRealtime(totalWait);
    }

    // 시작 코너 기준 대각선 계단 인덱스(같은 값이면 동시에 페이드)
    private int DiagonalStep(int x, int y)
    {
        int fx = _origin == Corner.TopRight || _origin == Corner.BottomRight ? (_columns - 1 - x) : x;
        int fy = _origin == Corner.BottomLeft || _origin == Corner.BottomRight ? (_rows - 1 - y) : y;
        return fx + fy;
    }

    private void BuildOverlay()
    {
        var canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // 항상 최상단
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGo.AddComponent<GraphicRaycaster>(); // 연출 중 입력 차단

        var gridGo = new GameObject("Grid");
        var gridRt = gridGo.AddComponent<RectTransform>();
        gridRt.SetParent(canvasGo.transform, false);
        Stretch(gridRt);

        // 앵커를 셀 비율로 나눠 배치 -> 화면 비율과 무관하게 항상 꽉 덮인다.
        // 경계 틈 방지를 위해 각 타일을 1px씩 확장(offset)한다.
        _tiles = new Image[_columns, _rows];
        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                var tileGo = new GameObject($"Tile_{x}_{y}");
                var rt = tileGo.AddComponent<RectTransform>();
                rt.SetParent(gridRt, false);

                // y=0을 화면 위쪽으로 두기 위해 세로 앵커를 뒤집는다.
                float ax0 = (float)x / _columns;
                float ax1 = (float)(x + 1) / _columns;
                float ay0 = 1f - (float)(y + 1) / _rows;
                float ay1 = 1f - (float)y / _rows;
                rt.anchorMin = new Vector2(ax0, ay0);
                rt.anchorMax = new Vector2(ax1, ay1);
                rt.offsetMin = new Vector2(-1f, -1f);
                rt.offsetMax = new Vector2(1f, 1f);

                var img = tileGo.AddComponent<Image>();
                img.color = _tileColor;
                img.raycastTarget = false;
                _tiles[x, y] = img;
            }
        }
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void SetAllVisible(bool visible)
    {
        float a = visible ? 1f : 0f;
        for (int y = 0; y < _rows; y++)
        for (int x = 0; x < _columns; x++)
        {
            var c = _tileColor;
            c.a = a;
            _tiles[x, y].color = c;
        }
    }
}
