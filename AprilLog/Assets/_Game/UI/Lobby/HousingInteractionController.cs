//담당자: 조규민

using UnityEngine;

/// <summary>
/// 하우징 가구 상호작용 MVP 객체를 생성하고 연결합니다.
/// </summary>
public class HousingInteractionController : MonoBehaviour
{
    [Header("가구 상호작용 View")]
    [SerializeField] private HousingInteractionView[] _interactionViews;
    [SerializeField] private HousingInteractionExitView _exitView;

    [Header("플레이어 이동 연결")]
    [SerializeField] private HousingPlayerMoveController _playerMoveController;

    private HousingInteractionModel _model;
    private HousingInteractionPresenter _presenter;

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

    private void HandlePausePlayerMovement()
    {
        _playerMoveController?.PauseMovement();
    }

    private void HandleResumePlayerMovement(bool _restoreStartPosition)
    {
        _playerMoveController?.ResumeMovement(_restoreStartPosition);
    }
}
