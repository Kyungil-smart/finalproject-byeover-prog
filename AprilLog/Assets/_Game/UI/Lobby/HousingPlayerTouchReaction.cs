//담당자: 조규민
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 하우징 플레이어 터치 횟수에 따라 반응 문구를 표시합니다.
/// </summary>
public class HousingPlayerTouchReaction : MonoBehaviour
{
    private const float DisplaySeconds = 2f;
    private const float TouchCountResetSeconds = 3f;

    [Header("터치 영역")]
    [Tooltip("플레이어 터치 판정에 사용할 RectTransform입니다.")]
    [SerializeField] private RectTransform _touchArea;

    [Header("반응 문구")]
    [Tooltip("플레이어 위에 표시할 반응 텍스트입니다.")]
    [SerializeField] private TextMeshProUGUI _reactionText;

    private int _touchCount;
    private float _hideTimer;
    private float _resetTimer;

    private void Awake()
    {
        ResolveMissingReferences();
        HideReaction();
    }

    private void Update()
    {
        HandleInput();
        UpdateHideTimer();
        UpdateResetTimer();
    }

    private void HandleInput()
    {
        if (_touchArea == null)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame == true)
        {
            TryTouchPlayer(Mouse.current.position.ReadValue());
            return;
        }

        if (Touchscreen.current == null)
        {
            return;
        }

        if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame == false)
        {
            return;
        }

        TryTouchPlayer(Touchscreen.current.primaryTouch.position.ReadValue());
    }

    private void TryTouchPlayer(Vector2 _screenPosition)
    {
        Camera _camera = GetEventCamera();

        if (RectTransformUtility.RectangleContainsScreenPoint(_touchArea, _screenPosition, _camera) == false)
        {
            return;
        }

        HandlePlayerTouched();
    }

    private void HandlePlayerTouched()
    {
        _touchCount++;
        _hideTimer = DisplaySeconds;
        _resetTimer = TouchCountResetSeconds;

        ShowReaction(GetReactionMessage(_touchCount));
    }

    private void UpdateHideTimer()
    {
        if (_reactionText == null)
        {
            return;
        }

        if (_reactionText.gameObject.activeSelf == false)
        {
            return;
        }

        _hideTimer -= Time.deltaTime;

        if (_hideTimer > 0f)
        {
            return;
        }

        HideReaction();
    }

    private void UpdateResetTimer()
    {
        if (_touchCount <= 0)
        {
            return;
        }

        _resetTimer -= Time.deltaTime;

        if (_resetTimer > 0f)
        {
            return;
        }

        _touchCount = 0;
    }

    private void ShowReaction(string _message)
    {
        if (_reactionText == null)
        {
            Debug.LogWarning("[HousingPlayerTouchReaction] 반응 텍스트가 연결되지 않았습니다.", this);
            return;
        }

        _reactionText.text = _message;
        _reactionText.gameObject.SetActive(true);
    }

    private void HideReaction()
    {
        if (_reactionText == null)
        {
            return;
        }

        _reactionText.gameObject.SetActive(false);
    }

    private string GetReactionMessage(int _count)
    {
        if (_count <= 1)
        {
            return "어... 갑자기 왜 그래?";
        }

        if (_count == 2)
        {
            return "아, 또 와줬구나!";
        }

        if (_count == 3)
        {
            return "잠깐만... 너무 자주 누르는 거 아니야?";
        }

        return "으으... 너무 정신없어...";
    }

    private void ResolveMissingReferences()
    {
        if (_touchArea == null)
        {
            _touchArea = GetComponent<RectTransform>();
        }

        if (_reactionText != null)
        {
            return;
        }

        _reactionText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private Camera GetEventCamera()
    {
        Canvas _canvas = GetComponentInParent<Canvas>();

        if (_canvas == null)
        {
            return null;
        }

        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return _canvas.worldCamera;
    }
}
