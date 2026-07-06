//담당자: 조규민
// 부팅·로그인 Canvas 배경을 화면 비율에 맞게 확대하고 씬 로드 시 자동 연결

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 배경 이미지를 원본 비율로 유지하면서 빈 여백 없이 화면 전체를 덮습니다.
/// </summary>
public class BootFullscreenBackgroundView : MonoBehaviour
{
    [Header("배경 이미지")]
    [Tooltip("비율을 유지하며 화면을 덮을 배경 Image입니다. 비워두면 현재 오브젝트의 Image를 사용합니다.")]
    [SerializeField] private Image _targetImage;

    private RectTransform _targetRectTransform;
    private RectTransform _parentRectTransform;
    private Vector2 _lastParentSize;
    private Sprite _lastSprite;

    public void Initialize(Image _image)
    {
        _targetImage = _image;
        CacheReferences();
        ApplyCoverSize();
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        Canvas.willRenderCanvases += ApplyCoverSize;
        ApplyCoverSize();
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= ApplyCoverSize;
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyCoverSize();
    }

    // 배경 Image와 RectTransform 참조 자동 탐색
    private void CacheReferences()
    {
        if (_targetImage == null)
        {
            _targetImage = GetComponent<Image>();
        }

        if (_targetImage == null)
        {
            return;
        }

        _targetRectTransform = _targetImage.rectTransform;
        _parentRectTransform = _targetRectTransform.parent as RectTransform;
        _targetImage.preserveAspect = true;
        _targetImage.raycastTarget = false;
    }

    // 원본 이미지 비율을 유지하며 화면 전체를 덮는 크기 계산
    private void ApplyCoverSize()
    {
        if (_targetImage == null || _targetImage.sprite == null)
        {
            return;
        }

        if (_targetRectTransform == null || _parentRectTransform == null)
        {
            CacheReferences();
        }

        if (_targetRectTransform == null || _parentRectTransform == null)
        {
            return;
        }

        Vector2 _parentSize = _parentRectTransform.rect.size;

        if (_parentSize.x <= 0f || _parentSize.y <= 0f)
        {
            return;
        }

        if (_lastParentSize == _parentSize && _lastSprite == _targetImage.sprite)
        {
            return;
        }

        _lastParentSize = _parentSize;
        _lastSprite = _targetImage.sprite;

        Rect _spriteRect = _targetImage.sprite.rect;
        float _imageRatio = _spriteRect.width / _spriteRect.height;
        float _parentRatio = _parentSize.x / _parentSize.y;

        Vector2 _targetSize = _parentRatio > _imageRatio
            ? new Vector2(_parentSize.x, _parentSize.x / _imageRatio)
            : new Vector2(_parentSize.y * _imageRatio, _parentSize.y);

        _targetRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _targetRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _targetRectTransform.pivot = new Vector2(0.5f, 0.5f);
        _targetRectTransform.anchoredPosition = Vector2.zero;
        _targetRectTransform.sizeDelta = _targetSize;
        _targetRectTransform.SetAsFirstSibling();
    }
}

public static class BootFullscreenBackgroundRuntimeApplier
{
    private const string _bootSceneName = "_Boot";
    private const string _startCanvasName = "StartCanvas";
    private const string _loginCanvasName = "LoginCanvas";
    private const string _backgroundName = "Background";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyToScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        ApplyToScene(_scene);
    }

    // 로드된 씬의 시작·로그인 Canvas 배경에 화면 맞춤 기능 연결
    private static void ApplyToScene(Scene _scene)
    {
        if (!_scene.IsValid() || _scene.name != _bootSceneName)
        {
            return;
        }

        Canvas[] _canvases = Resources.FindObjectsOfTypeAll<Canvas>();

        foreach (Canvas _canvas in _canvases)
        {
            if (_canvas == null || _canvas.gameObject.scene.handle != _scene.handle)
            {
                continue;
            }

            if (_canvas.name == _startCanvasName)
            {
                ApplyStartCanvasBackground(_canvas);
                continue;
            }

            if (_canvas.name == _loginCanvasName)
            {
                ApplyLoginCanvasBackground(_canvas);
            }
        }
    }

    private static void ApplyStartCanvasBackground(Canvas _startCanvas)
    {
        Image _rootImage = _startCanvas.GetComponent<Image>();

        if (_rootImage == null || _rootImage.sprite == null)
        {
            return;
        }

        Image _backgroundImage = ResolveOrCreateBackgroundImage(_startCanvas.transform, _rootImage);
        _rootImage.enabled = false;
        AttachCoverView(_backgroundImage);
    }

    private static void ApplyLoginCanvasBackground(Canvas _loginCanvas)
    {
        Transform _backgroundTransform = _loginCanvas.transform.Find(_backgroundName);

        if (_backgroundTransform == null)
        {
            return;
        }

        Image _backgroundImage = _backgroundTransform.GetComponent<Image>();

        if (_backgroundImage == null)
        {
            return;
        }

        AttachCoverView(_backgroundImage);
    }

    private static Image ResolveOrCreateBackgroundImage(Transform _parent, Image _sourceImage)
    {
        Transform _backgroundTransform = _parent.Find(_backgroundName);
        Image _backgroundImage = null;

        if (_backgroundTransform != null)
        {
            _backgroundImage = _backgroundTransform.GetComponent<Image>();
        }

        if (_backgroundImage == null)
        {
            GameObject _backgroundObject = new GameObject(_backgroundName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _backgroundObject.layer = _parent.gameObject.layer;
            _backgroundObject.transform.SetParent(_parent, false);
            _backgroundImage = _backgroundObject.GetComponent<Image>();
        }

        _backgroundImage.sprite = _sourceImage.sprite;
        _backgroundImage.color = _sourceImage.color;
        _backgroundImage.material = _sourceImage.material;
        _backgroundImage.type = Image.Type.Simple;
        _backgroundImage.preserveAspect = true;
        _backgroundImage.raycastTarget = false;
        _backgroundImage.transform.SetAsFirstSibling();
        return _backgroundImage;
    }

    private static void AttachCoverView(Image _backgroundImage)
    {
        if (_backgroundImage == null)
        {
            return;
        }

        BootFullscreenBackgroundView _coverView = _backgroundImage.GetComponent<BootFullscreenBackgroundView>();

        if (_coverView == null)
        {
            _coverView = _backgroundImage.gameObject.AddComponent<BootFullscreenBackgroundView>();
        }

        _coverView.Initialize(_backgroundImage);
    }
}
