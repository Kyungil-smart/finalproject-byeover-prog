using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 씬 전환 화이트 방향 와이프 오버레이.
// 순서: 페이드아웃(오→왼 닦으며 덮기) → 로딩 캔버스 표시(이 구간에 비동기 로드/활성화 → 유니티 기본 프레임 은폐) → 페이드인(오→왼 닦으며 새 화면 노출).
// 완전히 덮인 구간에서 씬을 활성화하므로 파란 기본 배경이 노출되지 않는다.
// 씬에 미리 배치할 필요 없음 - 배치돼 있으면 그 인스턴스를 쓰고, 없으면 처음 호출 시 스스로 생성된다.
public class SceneTransition : MonoBehaviour
{
    [Header("페이드")]
    [Tooltip("전환 덮개 색 (기본 흰색)")]
    [SerializeField] private Color _fadeColor = Color.white;
    [Tooltip("현재 화면을 덮는 페이드아웃 시간")]
    [SerializeField] private float _fadeOutDuration = 0.35f;
    [Tooltip("새 화면을 드러내는 페이드인 시간")]
    [SerializeField] private float _fadeInDuration = 0.35f;
    [Tooltip("페이드 이징")]
    [SerializeField] private Ease _ease = Ease.InOutSine;

    [Header("와이프 질감")]
    [Tooltip("와이프 경계의 부드러운 그라데이션 폭(px)")]
    [SerializeField] private float _softEdgeWidth = 220f;
    [Tooltip("와이프 진행률에 맞춰 덮개 알파도 같이 올리고 내린다")]
    [SerializeField] private bool _useWipeAlphaFade = true;
    [Tooltip("와이프 시작/끝 지점의 최소 알파")]
    [SerializeField, Range(0f, 1f)] private float _minWipeAlpha = 0f;
    [Tooltip("화면이 완전히 덮였을 때의 최대 알파")]
    [SerializeField, Range(0f, 1f)] private float _maxWipeAlpha = 1f;

    [Header("로딩 캔버스")]
    [Tooltip("페이드 사이에 띄울 로딩 캔버스. 이 오브젝트의 자식으로 두면 씬 전환에도 유지된다")]
    [SerializeField] private GameObject _loadingCanvas;
    [Tooltip("실질 로딩이 없어도 로딩 캔버스를 최소 이만큼 보여준다")]
    [SerializeField] private float _minLoadingTime = 1.4f;
    [Tooltip("로딩 중 표시할 GIF/영상/스프라이트 애니메이션 오브젝트")]
    [SerializeField] private GameObject _loadingAnimationObject;
    [Tooltip("우측 하단 NOW LODING 텍스트. 비워두면 자동 생성한다")]
    [SerializeField] private TMP_Text _nowLoadingText;
    [Tooltip("우측 하단 로딩 텍스트 표시 여부")]
    [SerializeField] private bool _showNowLoadingText = true;
    [Tooltip("로딩 텍스트 기본 문구")]
    [SerializeField] private string _nowLoadingBaseText = "NOW LODING";
    [Tooltip("로딩 텍스트 점 애니메이션 간격")]
    [SerializeField] private float _nowLoadingDotInterval = 0.22f;
    [Tooltip("로드 완료 후 로딩 텍스트 점 애니메이션을 몇 사이클 더 보여줄지")]
    [SerializeField] private int _nowLoadingHoldCycles = 2;
    [Tooltip("흰 화면과 로딩 화면을 부드럽게 이어주는 시간")]
    [SerializeField] private float _loadingBlendDuration = 0.25f;
    [Tooltip("로딩 캔버스 뒤에 자동으로 깔 전체 화면 배경색")]
    [SerializeField] private Color _loadingBackgroundColor = Color.white;

    [Header("로딩바 (StartLoadingSlider 재사용)")]
    [Tooltip("로딩 진행률을 표시할 Slider. StartCanvas의 StartLoadingSlider를 로딩 캔버스로 옮겨 연결한다")]
    [SerializeField] private Slider _loadingSlider;
    [Tooltip("퍼센트 표시 텍스트(선택)")]
    [SerializeField] private TMP_Text _loadingPercentText;

    [Header("전체 타이밍/정렬")]
    [Tooltip("전환 전체가 최소 이 시간은 확보되도록 대기한다")]
    [SerializeField] private float _minTotalDuration = 2.1f;
    [Tooltip("전환 캔버스 정렬 순서. 로딩 캔버스는 이보다 위에 두고 불투명 흰 화면 위에서 페이드한다")]
    [SerializeField] private int _sortingOrder = 30000;

    private static SceneTransition _instance;
    private Canvas _canvas;
    private Image _fade;
    private RectTransform _fadeRect;
    private Image _fadeEdge;
    private RectTransform _fadeEdgeRect;
    private Sprite _fadeEdgeSprite;
    private Sprite _fadeEdgeReverseSprite;
    private Texture2D _fadeEdgeTexture;
    private Texture2D _fadeEdgeReverseTexture;
    private Tween _fadeTween;
    private float _currentWipeWidth;
    private bool _wipeAnchoredRight = true;
    private bool _isPlaying;
    private Coroutine _nowLoadingRoutine;
    private CanvasGroup _loadingCanvasGroup;
    private Image _loadingBackgroundImage;
    private int _nowLoadingFrameIndex;

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

    /// <summary>지정 씬을 화이트 페이드 + 로딩 캔버스 연출과 함께 로드한다.</summary>
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
        SetupLoadingCanvas();
        BuildNowLoadingTextIfNeeded();
        SetIdle();
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
        float startTime = Time.unscaledTime;

        _canvas.gameObject.SetActive(true);

        // ── 페이드 아웃 ── 오른쪽에서 왼쪽으로 흰 화면이 닦으며 현재 화면을 덮는다.
        yield return WipeOut(_fadeOutDuration);
        SetFullCoverAlpha(1f); // 흰 화면 전체 불투명 고정. 로딩 내내 유지되어 이전(로그인) 씬이 절대 노출되지 않는다.

        // ── 로딩 캔버스 ── 불투명 흰 화면 '위'에서 부드럽게 나타난다.
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        ShowLoadingCanvas(true);
        SetLoadingCanvasAlpha(0f);
        StartNowLoadingText();
        SetBarFill(0f);
        yield return FadeLoadingCanvas(0f, 1f, _loadingBlendDuration);

        float loadStart = Time.unscaledTime;
        while (op.progress < 0.9f)
        {
            SetBarFill(op.progress / 0.9f);
            yield return null;
        }
        while (Time.unscaledTime - loadStart < _minLoadingTime)
            yield return null;
        SetBarFill(1f);
        yield return WaitForNowLoadingDotCycleEnd();

        // 전체 최소 시간(2.1초) 확보 - 페이드인 전에 남은 시간 대기
        float remaining = _minTotalDuration - (Time.unscaledTime - startTime) - _fadeInDuration;
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        // 새 씬 활성화 (로딩 캔버스 + 불투명 흰 화면이 가리고 있음)
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        // 로딩 캔버스를 걷어내면 그 아래 불투명 흰 화면이 새 씬을 덮고 있다.
        yield return FadeLoadingCanvas(1f, 0f, _loadingBlendDuration);
        StopNowLoadingText();
        ShowLoadingCanvas(false);
        SetFullCoverAlpha(1f);

        // ── 페이드 인 ── 오른쪽에서 왼쪽으로 흰 화면이 걷히며 새 화면을 드러낸다.
        yield return WipeIn(_fadeInDuration);

        SetIdle();
        _isPlaying = false;
    }

    private IEnumerator WipeOut(float duration)
    {
        KillFadeTween();
        _wipeAnchoredRight = true;
        SetFadeWipe(0f);
        _fadeTween = DOTween.To(() => _currentWipeWidth, SetFadeWipe, 1f, duration).SetEase(_ease).SetUpdate(true);
        yield return new WaitForSecondsRealtime(duration);
        SetFadeWipe(1f);
    }

    private IEnumerator WipeIn(float duration)
    {
        KillFadeTween();
        _wipeAnchoredRight = false;
        SetFadeWipe(1f);
        _fadeTween = DOTween.To(() => _currentWipeWidth, SetFadeWipe, 0f, duration).SetEase(_ease).SetUpdate(true);
        yield return new WaitForSecondsRealtime(duration);
        _wipeAnchoredRight = true;
        SetFadeWipe(0f);
    }

    private void KillFadeTween()
    {
        if (_fadeTween != null && _fadeTween.IsActive())
            _fadeTween.Kill();
        _fadeTween = null;
    }

    private IEnumerator FadeFullCover(float fromAlpha, float toAlpha, float duration)
    {
        KillFadeTween();
        SetFullCoverAlpha(fromAlpha);
        if (duration <= 0f)
        {
            SetFullCoverAlpha(toAlpha);
            yield break;
        }

        float alpha = fromAlpha;
        _fadeTween = DOTween.To(() => alpha, x =>
        {
            alpha = x;
            SetFullCoverAlpha(x);
        }, toAlpha, duration).SetEase(_ease).SetUpdate(true);
        yield return new WaitForSecondsRealtime(duration);
        SetFullCoverAlpha(toAlpha);
    }

    // 로딩 캔버스(불투명 흰 화면 위)를 CanvasGroup 알파로 페이드
    private IEnumerator FadeLoadingCanvas(float fromAlpha, float toAlpha, float duration)
    {
        if (_loadingCanvasGroup == null || duration <= 0f)
        {
            SetLoadingCanvasAlpha(toAlpha);
            yield break;
        }

        SetLoadingCanvasAlpha(fromAlpha);
        float alpha = fromAlpha;
        var tween = DOTween.To(() => alpha, x =>
        {
            alpha = x;
            SetLoadingCanvasAlpha(x);
        }, toAlpha, duration).SetEase(_ease).SetUpdate(true);
        yield return new WaitForSecondsRealtime(duration);
        tween.Kill();
        SetLoadingCanvasAlpha(toAlpha);
    }

    private void ShowLoadingCanvas(bool show)
    {
        if (_loadingCanvas != null)
            _loadingCanvas.SetActive(show);
        if (_loadingAnimationObject != null)
            _loadingAnimationObject.SetActive(show);
        if (_nowLoadingText != null)
            _nowLoadingText.gameObject.SetActive(show && _showNowLoadingText);
    }

    private void StartNowLoadingText()
    {
        if (!_showNowLoadingText || _nowLoadingText == null)
            return;

        StopNowLoadingText();
        _nowLoadingRoutine = StartCoroutine(AnimateNowLoadingText());
    }

    private void StopNowLoadingText()
    {
        if (_nowLoadingRoutine != null)
        {
            StopCoroutine(_nowLoadingRoutine);
            _nowLoadingRoutine = null;
        }
    }

    private IEnumerator AnimateNowLoadingText()
    {
        string[] dotFrames = { "·..", ".·.", "..·" };
        int index = 0;

        while (true)
        {
            _nowLoadingFrameIndex = index;
            if (_nowLoadingText != null)
                _nowLoadingText.text = _nowLoadingBaseText + dotFrames[index];

            index = (index + 1) % dotFrames.Length;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, _nowLoadingDotInterval));
        }
    }

    private IEnumerator WaitForNowLoadingDotCycleEnd()
    {
        if (!_showNowLoadingText || _nowLoadingText == null)
            yield break;

        int requiredCycles = Mathf.Max(1, _nowLoadingHoldCycles);
        int completedCycles = 0;
        bool sawCycleProgress = false;
        float timeout = Mathf.Max(0.1f, _nowLoadingDotInterval * (requiredCycles * 3f + 2f));
        float startTime = Time.unscaledTime;

        while (Time.unscaledTime - startTime < timeout)
        {
            if (_nowLoadingFrameIndex != 0)
                sawCycleProgress = true;
            if (sawCycleProgress && _nowLoadingFrameIndex == 0)
            {
                completedCycles++;
                if (completedCycles >= requiredCycles)
                    yield break;

                sawCycleProgress = false;
            }

            yield return null;
        }
    }

    private void SetBarFill(float v)
    {
        v = Mathf.Clamp01(v);
        if (_loadingSlider != null)
            _loadingSlider.value = Mathf.Lerp(_loadingSlider.minValue, _loadingSlider.maxValue, v);
        if (_loadingPercentText != null)
            _loadingPercentText.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    // 대기 상태: 오버레이 비활성화(렌더/입력 없음)
    private void SetIdle()
    {
        StopNowLoadingText();
        SetFadeWipe(0f);
        ShowLoadingCanvas(false);
        SetBarFill(0f);
        _canvas.gameObject.SetActive(false);
    }

    private void SetFadeWipe(float width01)
    {
        width01 = Mathf.Clamp01(width01);
        _currentWipeWidth = width01;

        var c = _fadeColor;
        c.a = GetWipeAlpha(width01);
        _fade.color = c;
        if (_wipeAnchoredRight)
        {
            _fadeRect.anchorMin = new Vector2(1f - width01, 0f);
            _fadeRect.anchorMax = Vector2.one;
        }
        else
        {
            _fadeRect.anchorMin = Vector2.zero;
            _fadeRect.anchorMax = new Vector2(width01, 1f);
        }
        _fadeRect.offsetMin = Vector2.zero;
        _fadeRect.offsetMax = Vector2.zero;

        if (_fadeEdge == null || _fadeEdgeRect == null)
            return;

        _fadeEdge.color = c;
        _fadeEdge.enabled = width01 > 0f && width01 < 1f && _softEdgeWidth > 0f;
        if (_wipeAnchoredRight)
        {
            _fadeEdge.sprite = _fadeEdgeSprite;
            _fadeEdgeRect.anchorMin = new Vector2(1f - width01, 0f);
            _fadeEdgeRect.anchorMax = new Vector2(1f - width01, 1f);
            _fadeEdgeRect.pivot = new Vector2(1f, 0.5f);
        }
        else
        {
            _fadeEdge.sprite = _fadeEdgeReverseSprite;
            _fadeEdgeRect.anchorMin = new Vector2(width01, 0f);
            _fadeEdgeRect.anchorMax = new Vector2(width01, 1f);
            _fadeEdgeRect.pivot = new Vector2(0f, 0.5f);
        }
        _fadeEdgeRect.anchoredPosition = Vector2.zero;
        _fadeEdgeRect.sizeDelta = new Vector2(_softEdgeWidth, 0f);
    }

    private void SetFullCoverAlpha(float alpha)
    {
        _currentWipeWidth = 1f;
        _wipeAnchoredRight = false;
        Stretch(_fadeRect);

        var c = _fadeColor;
        c.a = Mathf.Clamp01(alpha);
        _fade.color = c;

        if (_fadeEdge != null)
            _fadeEdge.enabled = false;
    }

    private void SetLoadingCanvasAlpha(float alpha)
    {
        if (_loadingCanvasGroup == null)
            return;

        _loadingCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private float GetWipeAlpha(float width01)
    {
        if (!_useWipeAlphaFade)
            return _maxWipeAlpha;

        float minAlpha = Mathf.Min(_minWipeAlpha, _maxWipeAlpha);
        float maxAlpha = Mathf.Max(_minWipeAlpha, _maxWipeAlpha);
        return Mathf.Lerp(minAlpha, maxAlpha, Mathf.Clamp01(width01));
    }

    // ---------- 오버레이 생성 ----------

    private void BuildOverlay()
    {
        var canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = _sortingOrder;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGo.AddComponent<GraphicRaycaster>();

        var fadeGo = new GameObject("Fade");
        _fadeRect = fadeGo.AddComponent<RectTransform>();
        _fadeRect.SetParent(canvasGo.transform, false);
        Stretch(_fadeRect);
        _fade = fadeGo.AddComponent<Image>();
        _fade.color = _fadeColor;
        _fade.raycastTarget = true; // 전환 중 입력 차단

        var edgeGo = new GameObject("SoftEdge");
        _fadeEdgeRect = edgeGo.AddComponent<RectTransform>();
        _fadeEdgeRect.SetParent(canvasGo.transform, false);
        _fadeEdge = edgeGo.AddComponent<Image>();
        _fadeEdge.sprite = CreateSoftEdgeSprite();
        _fadeEdgeReverseSprite = CreateSoftEdgeSprite(true);
        _fadeEdge.type = Image.Type.Simple;
        _fadeEdge.raycastTarget = false;
    }

    private Sprite CreateSoftEdgeSprite(bool reverse = false)
    {
        const int width = 64;
        const int height = 1;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int x = 0; x < width; x++)
        {
            float t = (float)x / (width - 1);
            float alpha = reverse ? Mathf.SmoothStep(1f, 0f, t) : Mathf.SmoothStep(0f, 1f, t);
            texture.SetPixel(x, 0, new Color(1f, 1f, 1f, alpha));
        }

        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0f, 0.5f), 100f);
        if (reverse)
        {
            _fadeEdgeReverseTexture = texture;
        }
        else
        {
            _fadeEdgeTexture = texture;
            _fadeEdgeSprite = sprite;
        }
        return sprite;
    }

    // 로딩 캔버스를 이 오브젝트 밑으로 옮겨 씬 전환에도 유지되게 한다.
    private void SetupLoadingCanvas()
    {
        if (_loadingCanvas == null)
            _loadingCanvas = ResolveSceneLoadingCanvas();
        if (_loadingCanvas == null)
            _loadingCanvas = CreateDefaultLoadingCanvas();

        _loadingCanvas.transform.SetParent(transform, false);

        var lc = _loadingCanvas.GetComponent<Canvas>() ?? _loadingCanvas.AddComponent<Canvas>();
        lc.renderMode = RenderMode.ScreenSpaceOverlay;
        lc.overrideSorting = true;
        lc.sortingOrder = _sortingOrder + 10; // 불투명 흰 화면 '위'에 그려 그 알파로 나타났다 사라지게 한다.

        var scaler = _loadingCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = _loadingCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        if (_loadingCanvas.GetComponent<GraphicRaycaster>() == null)
            _loadingCanvas.AddComponent<GraphicRaycaster>();

        _loadingCanvasGroup = _loadingCanvas.GetComponent<CanvasGroup>();
        if (_loadingCanvasGroup == null)
            _loadingCanvasGroup = _loadingCanvas.AddComponent<CanvasGroup>();
        _loadingCanvasGroup.alpha = 1f;

        BuildLoadingBackgroundIfNeeded();
    }

    private GameObject ResolveSceneLoadingCanvas()
    {
        var bootLoadingView = FindFirstObjectByType<BootLoadingVideoView>(FindObjectsInactive.Include);
        if (bootLoadingView != null)
            return bootLoadingView.gameObject;

        return null;
    }

    private GameObject CreateDefaultLoadingCanvas()
    {
        var go = new GameObject("LoadingCanvas");
        go.transform.SetParent(transform, false);
        return go;
    }

    private void BuildLoadingBackgroundIfNeeded()
    {
        if (_loadingCanvas == null)
            return;

        var bgGo = new GameObject("LoadingAutoBackground");
        var rt = bgGo.AddComponent<RectTransform>();
        rt.SetParent(_loadingCanvas.transform, false);
        Stretch(rt);
        bgGo.transform.SetAsFirstSibling();

        _loadingBackgroundImage = bgGo.AddComponent<Image>();
        _loadingBackgroundImage.color = _loadingBackgroundColor;
        _loadingBackgroundImage.raycastTarget = false;
    }

    private void BuildNowLoadingTextIfNeeded()
    {
        if (_nowLoadingText != null)
            return;

        Transform parent = _loadingCanvas != null ? _loadingCanvas.transform : _canvas.transform;

        var textGo = new GameObject("NowLoadingText");
        var rt = textGo.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.one;
        rt.anchorMax = Vector2.one;
        rt.pivot = Vector2.one;
        rt.anchoredPosition = new Vector2(-64f, -64f);
        rt.sizeDelta = new Vector2(420f, 80f);

        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.BottomRight;
        text.fontSize = 34f;
        text.color = _fadeColor;
        text.raycastTarget = false;
        text.text = _nowLoadingBaseText + "...";
        _nowLoadingText = text;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
