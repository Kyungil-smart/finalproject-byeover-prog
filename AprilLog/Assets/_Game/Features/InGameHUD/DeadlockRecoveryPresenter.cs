//담당자: 조규민

using UnityEngine;

/// <summary>
/// 데드락 감지 이벤트와 안내 팝업의 복구 선택 흐름을 연결한다.
/// </summary>
public class DeadlockRecoveryPresenter
{
    private const string _deadlockMessage = "더 이상 책을 정리할 수 없습니다!\n\n경험치 10%를 잃고, 퍼즐을 재생성합니다.";

    private readonly SortSystem _sortSystem;
    private readonly InGameGrowthSystem _growthSystem;
    private readonly InGameConfirmPopupPresenter _popupPresenter;

    public DeadlockRecoveryPresenter(
        SortSystem sortSystem,
        InGameGrowthSystem growthSystem,
        InGameConfirmPopupPresenter popupPresenter)
    {
        _sortSystem = sortSystem;
        _growthSystem = growthSystem;
        _popupPresenter = popupPresenter;

        if (_sortSystem != null)
        {
            _sortSystem.OnDeadlockDetected += HandleDeadlockDetected;
        }
    }

    public void Dispose()
    {
        if (_sortSystem != null)
        {
            _sortSystem.OnDeadlockDetected -= HandleDeadlockDetected;
        }
    }

    private void HandleDeadlockDetected()
    {
        if (_popupPresenter == null)
        {
            Debug.LogWarning("[DeadlockRecoveryPresenter] 데드락 안내 팝업이 없어 복구 선택을 처리할 수 없습니다.");
            _sortSystem?.CancelDeadlockRecovery();
            return;
        }

        _popupPresenter.Open(_deadlockMessage, ConfirmRecovery, CancelRecovery);
    }

    private void ConfirmRecovery()
    {
        if (_growthSystem == null)
        {
            Debug.LogWarning("[DeadlockRecoveryPresenter] InGameGrowthSystem이 없어 데드락 경험치 패널티를 적용하지 못했습니다.");
        }
        else
        {
            _growthSystem.ApplyDeadlockPenalty();
        }

        _sortSystem?.RecoverFromDeadlock();
    }

    private void CancelRecovery()
    {
        _sortSystem?.CancelDeadlockRecovery();
    }
}
