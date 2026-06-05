// 담당자 : 정승우
// 설명   : 인게임 HUD Presenter -- Model 구독해서 View 갱신

// 수정자 : 정승우
// 수정내용 : Model 참조가 비어 있을 때 이벤트 구독 NullReference 방지

// 수정자 : 김영찬
// 수정내용 : 인게임 UI에 넘겨줄 정보 최신화

using UnityEngine;

public class InGameHUDPresenter
{
    private readonly IInGameHUDView _view;
    private readonly PlayerModel _player;
    private readonly ComboModel _combo;
    private readonly InGameGrowthSystem _growthSystem;
    private readonly StageLoopManager _loopManager;
    private readonly StageModel _stage;

    public InGameHUDPresenter(IInGameHUDView view, PlayerModel player, ComboModel combo, InGameGrowthSystem growthSystem, StageLoopManager loopManager,StageModel stage)
    {
        _view = view;
        _player = player;
        _combo = combo;
        _growthSystem = growthSystem;
        _loopManager = loopManager;
        _stage = stage;

        if (player != null)
        {
            _player.OnHPChanged += HandleHp;
        }

        if (combo != null)
        {
            _combo.OnComboChanged += HandleCombo;
        }

        if (growthSystem != null)
        {
            _growthSystem.OnEXPChanged += HandleExp;
            _growthSystem.OnLevelUp += HandleLevel;
        }

        if (loopManager != null)
        {
            _loopManager.OnStageChanged += HandleStageProgress;
        }

        if (stage != null)
        {
            _stage.OnTimeChanged += HandleTimer;
            _stage.OnWaveStateChanged += HandleWaveState;
            _stage.OnSpecialWaveEntered += HandleSpecialWaveEntered;
        }
    }

    public void Dispose()
    {
        if (_player != null)
        {
            _player.OnHPChanged -= HandleHp;
        }

        if (_combo != null)
        {
            _combo.OnComboChanged -= HandleCombo;
        }

        if (_growthSystem != null)
        {
            _growthSystem.OnEXPChanged -= HandleExp;
            _growthSystem.OnLevelUp -= HandleLevel;
        }

        if (_loopManager != null)
        {
            _loopManager.OnStageChanged -= HandleStageProgress;
        }

        if (_stage != null)
        {
            _stage.OnTimeChanged -= HandleTimer;
            _stage.OnWaveStateChanged -= HandleWaveState;
            _stage.OnSpecialWaveEntered -= HandleSpecialWaveEntered;
        }
    }

    // ---------- 이벤트 핸들러 ----------
    private void HandleHp(int cur, int max)
    {
        _view.UpdateHP((float)cur / Mathf.Max(1, max));
        _view.UpdateHPText(cur, max);
    }
    
    private void HandleExp(int cur, int max)
    {
        _view.UpdateEXP((float)cur / Mathf.Max(1, max));
        _view.UpdateEXPText(cur, max);
    }

    private void HandleLevel(int current) => _view.UpdateLevelText(current);
    private void HandleCombo(int count) => _view.UpdateCombo(count);
    private void HandleTimer(float remainTime) => _view.UpdateStageTimer(remainTime);
    private void HandleWaveState(StageModel.WaveState state) => _view.UpdateWaveStateText(state);
    private void HandleSpecialWaveEntered(StageModel.SpawnType type) => _view.UpdateSpecialWavePopup(type);
    private void HandleStageProgress(int stageId) => _view.UpdateStageProgress(stageId);
}
