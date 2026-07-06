//담당자: 조규민
// 플레이어 터치마다 부모 Canvas를 탐색하지 않도록 이벤트 카메라 참조를 초기화 시 캐싱
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 하우징 플레이어 터치 횟수에 따라 반응 문구를 표시합니다.
/// </summary>
// 화면 입력의 플레이어 충돌 판정과 연속 터치 반응 문구 표시
// 반응 유지 시간 종료 또는 비활성화 시 표시 상태 초기화
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
    private Canvas _parentCanvas;

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

    // 마우스와 모바일 터치 입력을 공통 화면 좌표로 변환
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

    // 화면 좌표 Raycast 결과의 플레이어 충돌 여부 검사
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

    // Presenter 반응 문구를 말풍선 UI에 표시
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

    // 반응 유지 시간 종료 시 말풍선 UI 숨김
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
        if (_parentCanvas == null)
        {
            _parentCanvas = GetComponentInParent<Canvas>();
        }

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
        if (_parentCanvas == null)
        {
            return null;
        }

        if (_parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return _parentCanvas.worldCamera;
    }
}
