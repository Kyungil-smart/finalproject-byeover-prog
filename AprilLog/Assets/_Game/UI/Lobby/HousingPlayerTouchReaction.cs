//담당자: 조규민
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 하우징 플레이어 터치 횟수에 따라 반응 문구를 표시합니다.
/// </summary>
public class HousingPlayerTouchReaction : MonoBehaviour
{
    [Header("터치 영역")]
    [Tooltip("플레이어 터치 판정에 사용할 RectTransform입니다.")]
    [SerializeField] private RectTransform _touchArea;

    [Header("반응 말풍선")]
    [Tooltip("플레이어 위에 표시할 말풍선 루트입니다.")]
    [SerializeField] private GameObject _reactionBubble;
    [Tooltip("말풍선 안에 표시할 반응 텍스트입니다.")]
    [SerializeField] private TextMeshProUGUI _reactionText;

    private HousingPlayerReactionPresenter _presenter;

    private void Awake()
    {
        _presenter = new HousingPlayerReactionPresenter();
        ResolveMissingReferences();
        HideReaction();
    }

    private void OnDisable()
    {
        _presenter?.Reset();
        HideReaction();
    }

    private void Update()
    {
        HandleInput();

        if (_presenter == null)
        {
            return;
        }

        if (_presenter.Update(Time.deltaTime) == false)
        {
            return;
        }

        HideReaction();
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
        if (_presenter == null)
        {
            _presenter = new HousingPlayerReactionPresenter();
        }

        ShowReaction(_presenter.HandleTouched());
    }

    private void ShowReaction(string _message)
    {
        if (_reactionBubble == null || _reactionText == null)
        {
            Debug.LogWarning("[HousingPlayerTouchReaction] 반응 말풍선 연결을 확인해 주세요.", this);
            return;
        }

        _reactionText.text = _message;
        _reactionBubble.SetActive(true);
    }

    private void HideReaction()
    {
        if (_reactionBubble == null)
        {
            return;
        }

        _reactionBubble.SetActive(false);
    }

    private void ResolveMissingReferences()
    {
        if (_touchArea == null)
        {
            _touchArea = GetComponent<RectTransform>();
        }

        if (_reactionBubble == null && _reactionText != null)
        {
            _reactionBubble = _reactionText.transform.parent != transform
                ? _reactionText.transform.parent.gameObject
                : _reactionText.gameObject;
        }

        if (_reactionText != null)
        {
            return;
        }

        _reactionText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (_reactionBubble == null && _reactionText != null)
        {
            _reactionBubble = _reactionText.transform.parent != transform
                ? _reactionText.transform.parent.gameObject
                : _reactionText.gameObject;
        }
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
