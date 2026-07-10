// 작성자 : 홍정옥
// 플레이어 머리 위에 붙는 스크린스페이스 말풍선.
// 월드 앵커(플레이어)를 매 프레임 화면 좌표로 변환해 추종하고, 타이핑/클릭 진행을 처리한다.
// timeScale=0(대화 중 정지) 전제라 대기는 전부 Realtime

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InGameTalkBubble : MonoBehaviour, IPointerClickHandler
{
    public event Action OnAdvanceRequested;   // 텍스트 완성 후 클릭 → 다음 줄

    [Header("UI 참조")]
    [Tooltip("위치를 움직일 말풍선 본체(RectTransform).")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _dialogueText;

    [Header("연출")]
    [Tooltip("초당 출력 글자 수.")]
    [SerializeField] private float _charsPerSecond = 30f;
    [Tooltip("앵커(플레이어) 기준 말풍선을 띄울 월드 높이.")]
    [SerializeField] private float _worldYOffset = 0.9f;
    [Tooltip("변환된 화면 좌표에 더할 추가 오프셋(px).")]
    [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 20f);

    [Header("다음 진행 화살표")]
    [Tooltip("텍스트가 다 나오면 위아래로 까딱이는 '터치해서 진행' 화살표(Icon_Next).")]
    [SerializeField] private RectTransform _nextIcon;
    [SerializeField] private float _nextBobAmplitude = 8f;   // 이동 폭(px)
    [SerializeField] private float _nextBobSpeed = 4f;        // 왕복 속도

    [Header("다음 표시 위치")]
    [Tooltip("Icon_Next를 말풍선 오른쪽 아래에서 얼마나 안쪽으로 둘지(px).")]
    [SerializeField] private Vector2 _nextIconInset = new Vector2(48f, 36f);

    [Header("화면 가장자리 보정")]
    [Tooltip("말풍선을 화면 안전 영역 가장자리에서 최소 이만큼 떨어뜨려 잘림을 막는다(px).")]
    [SerializeField] private float _screenEdgePadding = 24f;

    private Transform _anchor;
    private Camera _camera;
    private Coroutine _typing;
    private bool _isTyping;

    private Vector2 _nextIconBasePos;
    private float _nextBobTime;
    private bool _useFixedViewportPosition;
    private Vector2 _fixedViewportPosition;
    private Vector2 _fixedScreenOffset;
    private GameObject _advanceTouchArea;
    private readonly Vector3[] _worldCorners = new Vector3[4];
    private float _designWidth = -1f;   // 인스펙터에 설정된 원본 말풍선 폭. 화면이 넓으면 이 값으로 복원한다.

    public bool IsTyping => _isTyping;

    private void Awake()
    {
        if (_nextIcon != null)
        {
            ConfigureNextIconPosition();
            _nextIcon.gameObject.SetActive(false);
        }
    }

    public void Bind(Transform anchor, Camera cam)
    {
        _anchor = anchor;
        _camera = cam != null ? cam : (Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>());
    }

    public void UseAnchorPosition()
    {
        _useFixedViewportPosition = false;
    }

    public void UseViewportPosition(Vector2 viewportPosition, Vector2 screenOffset)
    {
        _useFixedViewportPosition = true;
        _fixedViewportPosition = viewportPosition;
        _fixedScreenOffset = screenOffset;
    }

    public void Show()
    {
        SetAdvanceTouchAreaActive(true);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        StopTyping();
        HideNextIcon();
        SetAdvanceTouchAreaActive(false);
        gameObject.SetActive(false);
    }

    public void PlayLine(string speakerName, string text)
    {
        Show();
        string safeSpeakerName = speakerName ?? string.Empty;
        string safeText = text ?? string.Empty;
        if (_nameText != null) _nameText.text = safeSpeakerName;

        StopTyping();
        HideNextIcon();
        if (_dialogueText == null) return;

        _dialogueText.text = safeText;
        ConfigureNextIconPosition();
        _typing = StartCoroutine(TypeRoutine());
    }

    private void ConfigureNextIconPosition()
    {
        if (_nextIcon == null) return;

        _nextIcon.anchorMin = new Vector2(1f, 0f);
        _nextIcon.anchorMax = new Vector2(1f, 0f);
        _nextIcon.pivot = new Vector2(1f, 0f);
        _nextIconBasePos = new Vector2(-Mathf.Abs(_nextIconInset.x), Mathf.Abs(_nextIconInset.y));
        _nextIcon.anchoredPosition = _nextIconBasePos;
    }

    private IEnumerator TypeRoutine()
    {
        _isTyping = true;
        _dialogueText.maxVisibleCharacters = 0;
        _dialogueText.ForceMeshUpdate();

        int total = _dialogueText.textInfo.characterCount;
        float interval = _charsPerSecond > 0f ? 1f / _charsPerSecond : 0f;

        int visible = 0;
        while (visible < total)
        {
            visible++;
            _dialogueText.maxVisibleCharacters = visible;
            if (interval > 0f) yield return new WaitForSecondsRealtime(interval);
            else               yield return null;
        }

        _dialogueText.maxVisibleCharacters = int.MaxValue;
        _isTyping = false;
        _typing = null;
        ShowNextIcon();
    }

    // 타이핑 중이면 즉시 전체 표시.
    public void CompleteText()
    {
        StopTyping();
        if (_dialogueText != null)
            _dialogueText.maxVisibleCharacters = int.MaxValue;
        ShowNextIcon();
    }

    private void ShowNextIcon()
    {
        if (_nextIcon == null) return;
        _nextBobTime = 0f;
        _nextIcon.anchoredPosition = _nextIconBasePos;
        _nextIcon.gameObject.SetActive(true);
    }

    private void HideNextIcon()
    {
        if (_nextIcon == null) return;
        _nextIcon.gameObject.SetActive(false);
    }

    private void StopTyping()
    {
        if (_typing != null) StopCoroutine(_typing);
        _typing = null;
        _isTyping = false;
    }

    private void LateUpdate()
    {
        FollowAnchor();
        BobNextIcon();
    }

    private void FollowAnchor()
    {
        if (_root == null) return;

        if (_useFixedViewportPosition)
        {
            _root.position = new Vector3(
                Screen.width * _fixedViewportPosition.x + _fixedScreenOffset.x,
                Screen.height * _fixedViewportPosition.y + _fixedScreenOffset.y,
                0f);
            ClampWithinSafeArea();
            return;
        }

        if (_anchor == null || _camera == null) return;

        Vector3 world = _anchor.position + new Vector3(0f, _worldYOffset, 0f);
        Vector3 screen = _camera.WorldToScreenPoint(world);
        // Screen Space - Overlay 캔버스에서는 RectTransform.position이 화면 픽셀 좌표.
        _root.position = screen + (Vector3)_screenOffset;
        ClampWithinSafeArea();
    }

    // 캐릭터가 화면 가장자리에 있으면 말풍선이 밖으로 잘리므로, 실제 크기(코너)를 재서
    // 안전 영역 안으로 위치를 밀어 넣는다. 기종별 화면 크기/비율/노치 차이를 흡수한다.
    private void ClampWithinSafeArea()
    {
        if (_root == null) return;

        if (_designWidth < 0f) _designWidth = _root.sizeDelta.x;

        Rect area = Screen.safeArea;
        float maxWidthPixels = area.width - _screenEdgePadding * 2f;

        // 말풍선 폭이 화면(안전 영역)보다 넓으면 위치만 밀어도 반대쪽이 잘린다.
        // 실제 픽셀 폭 대비 안전 영역 폭 비율로 sizeDelta를 줄이고, 여유가 생기면 원본 폭으로 되돌린다.
        _root.GetWorldCorners(_worldCorners); // Overlay 캔버스: 월드 좌표 = 화면 픽셀
        float pixelWidth = _worldCorners[2].x - _worldCorners[0].x;
        if (pixelWidth > 0f && _root.sizeDelta.x > 0f)
        {
            float pixelsPerUnit = pixelWidth / _root.sizeDelta.x;
            float maxWidthUnits = maxWidthPixels / pixelsPerUnit;
            float targetWidth = Mathf.Min(_designWidth, maxWidthUnits);
            if (!Mathf.Approximately(_root.sizeDelta.x, targetWidth))
            {
                _root.sizeDelta = new Vector2(targetWidth, _root.sizeDelta.y);
                _root.GetWorldCorners(_worldCorners);
            }
        }

        float minX = _worldCorners[0].x;
        float minY = _worldCorners[0].y;
        float maxX = _worldCorners[2].x;
        float maxY = _worldCorners[2].y;

        float left = area.xMin + _screenEdgePadding;
        float right = area.xMax - _screenEdgePadding;
        float bottom = area.yMin + _screenEdgePadding;
        float top = area.yMax - _screenEdgePadding;

        float dx = 0f;
        if (minX < left) dx = left - minX;
        else if (maxX > right) dx = right - maxX;

        float dy = 0f;
        if (minY < bottom) dy = bottom - minY;
        else if (maxY > top) dy = top - maxY;

        if (dx != 0f || dy != 0f)
            _root.position += new Vector3(dx, dy, 0f);
    }

    // 텍스트가 다 나온 뒤 화살표를 위아래로 왕복. 일시정지 중에도 움직이게 unscaled.
    private void BobNextIcon()
    {
        if (_nextIcon == null || !_nextIcon.gameObject.activeSelf) return;

        _nextBobTime += Time.unscaledDeltaTime * _nextBobSpeed;
        float bob = Mathf.Sin(_nextBobTime) * _nextBobAmplitude;
        _nextIcon.anchoredPosition = _nextIconBasePos + new Vector2(0f, bob);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AdvanceOrComplete();
    }

    private void OnDestroy()
    {
        if (_advanceTouchArea != null)
            Destroy(_advanceTouchArea);
    }

    private void AdvanceOrComplete()
    {
        if (_isTyping) CompleteText();          // 타이핑 중 → 즉시 완성
        else           OnAdvanceRequested?.Invoke();   // 완성 후 → 다음 줄
    }

    private void SetAdvanceTouchAreaActive(bool active)
    {
        EnsureAdvanceTouchArea();
        if (_advanceTouchArea != null)
            _advanceTouchArea.SetActive(active);
    }

    private void EnsureAdvanceTouchArea()
    {
        if (_advanceTouchArea != null) return;

        Transform parent = transform.parent;
        if (parent == null) return;

        _advanceTouchArea = new GameObject("TalkAdvanceTouchArea", typeof(RectTransform), typeof(Image), typeof(TalkAdvanceTouchArea));
        _advanceTouchArea.layer = gameObject.layer;
        RectTransform rt = _advanceTouchArea.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.SetAsLastSibling();

        Image image = _advanceTouchArea.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        _advanceTouchArea.GetComponent<TalkAdvanceTouchArea>().Bind(this);
    }

    private sealed class TalkAdvanceTouchArea : MonoBehaviour, IPointerClickHandler
    {
        private InGameTalkBubble _owner;

        public void Bind(InGameTalkBubble owner)
        {
            _owner = owner;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _owner?.AdvanceOrComplete();
        }
    }
}
