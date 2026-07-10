//담당자: 조규민

using UnityEngine;

/// <summary>
/// 씬에 배치된 데드락 안내 팝업과 인게임 시스템 참조를 연결한다.
/// </summary>
public class DeadlockRecoveryBinder : MonoBehaviour
{
    [Header("팝업")]
    [Tooltip("씬에 미리 배치한 인게임 확인 팝업 View")]
    [SerializeField] private InGameConfirmPopupView _popupView;

    [Header("시스템")]
    [SerializeField] private SortSystem _sortSystem;
    [SerializeField] private InGameGrowthSystem _growthSystem;

    private InGameConfirmPopupModel _popupModel;
    private InGameConfirmPopupPresenter _popupPresenter;
    private DeadlockRecoveryPresenter _deadlockPresenter;

    private void Awake()
    {
        ResolveReferences();

        if (_popupView == null || _sortSystem == null)
        {
            Debug.LogWarning("[DeadlockRecoveryBinder] 필수 참조가 없어 데드락 안내 팝업을 연결하지 못했습니다.");
            return;
        }

        _popupModel = new InGameConfirmPopupModel();
        _popupPresenter = new InGameConfirmPopupPresenter(_popupModel, _popupView);
        _deadlockPresenter = new DeadlockRecoveryPresenter(_sortSystem, _growthSystem, _popupPresenter);
    }

    private void OnDestroy()
    {
        _deadlockPresenter?.Dispose();
        _popupPresenter?.Dispose();
    }

    private void ResolveReferences()
    {
        if (_popupView == null)
        {
            _popupView = FindFirstObjectByType<InGameConfirmPopupView>(FindObjectsInactive.Include);
        }

        if (_sortSystem == null)
        {
            _sortSystem = FindFirstObjectByType<SortSystem>();
        }

        if (_growthSystem == null)
        {
            _growthSystem = FindFirstObjectByType<InGameGrowthSystem>();
        }
    }
}
