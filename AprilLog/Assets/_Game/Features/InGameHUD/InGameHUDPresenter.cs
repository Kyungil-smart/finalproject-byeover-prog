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
    private readonly StateFeedBackColorSO _feedBackColor;

    private float _onHitFeedBackTimer = -1;
    private bool _isFeedBackBarColorOrigin;
    private readonly Color _defaultColor = new Color(1f, 1f, 1f, 0);

    public InGameHUDPresenter(IInGameHUDView view, PlayerModel player, ComboModel combo, InGameGrowthSystem growthSystem, StageLoopManager loopManager,StageModel stage, StateFeedBackColorSO feedBackColor)
    {
        _view = view;
        _player = player;
        _combo = combo;
        _growthSystem = growthSystem;
        _loopManager = loopManager;
        _stage = stage;
        _feedBackColor = feedBackColor;

        if (player != null)
        {
            _player.OnHPChanged += HandleHp;
            _player.OnHit += HandleOnHit;
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

        SyncInitialState();
    }

    // Presenter는 Model 초기화 이후에 생성되므로, 구독만으론 이미 지나간 초기 이벤트를 못 받는다.
    // 생성 직후 현재 값을 한 번 강제로 끌어와 씬에 박힌 placeholder(예: "10,000", "Level 30")를 덮어쓴다.
    private void SyncInitialState()
    {
        if (_player != null)
            HandleHp(_player.CurrentHP, _player.MaxHP);

        if (_growthSystem != null)
            _growthSystem.EmitCurrentState();
        
        ResetEffectColor();
    }

    public void Dispose()
    {
        if (_player != null)
        {
            _player.OnHPChanged -= HandleHp;
            _player.OnHit -= HandleOnHit;
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
    private void HandleOnHit(float duration) => SetOnHit(duration);
    
    // ---------- 보조 함수 ----------
    public void Update(float deltaTime)
    {
        Tick(deltaTime);
        PlayEffect();
    }
    
    
    private void Tick(float deltaTime)
    {
        if(_onHitFeedBackTimer > 0)
            _onHitFeedBackTimer -= deltaTime;
    }

    private void SetOnHit(float duration)
    {
        _onHitFeedBackTimer = duration;
    }

    private void PlayEffect()
    {
        if (_onHitFeedBackTimer > 0)
        {
            if (_isFeedBackBarColorOrigin) 
            {
                ApplyEffectColor(_feedBackColor.GetOnHitColor());
            }
        }
        else 
        {
            if (!_isFeedBackBarColorOrigin)
            {
                ResetEffectColor();
            }
        }
    }

    private void ApplyEffectColor(Color color)
    {
        if (color == _defaultColor)
        {
            _view.SetFeedBackBarColor(color);
        }
        else
        {
            var alphaAdjustedHitColor = new Color(color.r, color.g, color.b, 150);
            _view.SetFeedBackBarColor(alphaAdjustedHitColor);
        }
        
        _isFeedBackBarColorOrigin = color == _defaultColor;
    }
    
    private void ResetEffectColor()
    {
        ApplyEffectColor(_defaultColor);
    }
}
