//담당자: 조규민

using UnityEngine;

/// <summary>
/// 하우징 가구 상호작용 MVP 객체를 생성하고 연결합니다.
/// </summary>
// 하우징 상호작용 Model·View·Presenter 생성과 수명 주기 정리
public class HousingInteractionController : MonoBehaviour
{
    [Header("가구 상호작용 View")]
    [SerializeField] private HousingInteractionView[] _interactionViews;
    [SerializeField] private HousingInteractionExitView _exitView;

    [Header("플레이어 이동 연결")]
    [SerializeField] private HousingPlayerMoveController _playerMoveController;

    private HousingInteractionModel _model;
    private HousingInteractionPresenter _presenter;

    // Model과 Presenter 생성 후 하우징 상호작용 기능 초기화
    private void Awake()
    {
        ResolveReferences();
        _model = new HousingInteractionModel();
        _presenter = new HousingInteractionPresenter(
            _model,
            _interactionViews,
            _exitView,
            HandlePausePlayerMovement,
            HandleResumePlayerMovement);
        _presenter.Initialize();
    }

    private void OnDisable()
    {
        _presenter?.Reset();
    }

    private void OnDestroy()
    {
        _presenter?.Release();
    }

    private void ResolveReferences()
    {
        if (_interactionViews == null || _interactionViews.Length == 0)
        {
            _interactionViews = GetComponentsInChildren<HousingInteractionView>(true);
        }

        if (_exitView == null)
        {
            _exitView = GetComponentInChildren<HousingInteractionExitView>(true);
        }

        if (_playerMoveController == null)
        {
            _playerMoveController = GetComponentInChildren<HousingPlayerMoveController>(true);
        }
    }

    // 가구 상호작용 시작 시 자동 순찰 일시 정지
    private void HandlePausePlayerMovement()
    {
        _playerMoveController?.PauseMovement();
    }

    // 상호작용 종료 시 선택적 시작 위치 복원과 순찰 재개
    private void HandleResumePlayerMovement(bool _restoreStartPosition)
    {
        _playerMoveController?.ResumeMovement(_restoreStartPosition);
    }
}
