// 담당자 : 정승우
// 설명   : 인게임 HUD View -- HP, EXP, 콤보, 진행도 표시

// 수정자 : 정승우
// 수정내용 : Model 참조가 비어 있을 때 Presenter 생성을 건너뛰어 테스트 씬 NullReference 방지

// 수정자 : 김영찬
// 수정내용 : 인게임 UI에 넘겨줄 정보 최신화

// 3차 수정자 : 조규민
// 수정 내용 : StageBootstrapper 초기화 전에도 HP/EXP HUD Presenter가 먼저 연결될 수 있도록 초기화 조건 완화

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인게임 HUD 화면 표시 담당. 로직 없음.
/// </summary>
public class InGameHUDView : MonoBehaviour, IInGameHUDView
{
    // ---------- SerializeField ----------
    [Header("데이터 참조")]
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private ComboModel _comboModel;
    [SerializeField] private StageLoopManager _loopManager;
    [SerializeField] private StageBootstrapper _stageBootstrapper;
    [SerializeField] private InGameGrowthSystem _growthSystem;
    [SerializeField] private StateFeedBackColorSO _feedBackColor;

    [Header("UI")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private Slider _expSlider;
    [SerializeField] private TMP_Text _expText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _comboText;
    [SerializeField] private TMP_Text _stageProgressTimer;
    [SerializeField] private TMP_Text _curChapterViwerText;
    [SerializeField] private TMP_Text _curStageViwerText;
    [SerializeField] private TMP_Text _waveStateText;
    [SerializeField] private Image _feedbackBar;
    
    // ---------- Dictionary ----------
    private readonly Dictionary<string, Dictionary<string, string>> _translationDictionary = new Dictionary<string, Dictionary<string, string>>
    {
        { "ko", new Dictionary<string, string>
        {
            {"_currentChapterLabelText", "Ch."},
            {"_currentStageLabelText", "Stage."},
            {"_currentLevelLabelText", "Lv."}
        }},
        { "en", new Dictionary<string, string>
        {
            {"_currentChapterLabelText", "Ch."},
            {"_currentStageLabelText", "Stage."},
            {"_currentLevelLabelText", "Lv."}
        }}
    };
    
    // ---------- Private ----------
    private InGameHUDPresenter _presenter;
    private StageModel _stageModel;
    private LocalizationManager _localizationManager;
    private bool _hasWarnedMissingCoreReferences;

    private string _currentChapterLabelText;
    private string _currentStageLabelText;
    private string _currentLevelLabelText;
    
    private bool _boundToStageBootstrapper;
    
    // ---------- 이벤트 ----------
    public event Action OnComboTimerActive;
    public event Action<StageModel.SpawnType> OnSpecialWaveActive;
    
    // ---------- Const Text ----------
    private const string REMAIN_TIME_TEMP_TEXT = "남은 시간";
    private const string TRANSITION_TIME_TEMP_TEXT = "다음 웨이브 대기";

    private const string COMBO_TEXT = "COMBO";

    // ---------- 생명주기 ----------
    private void OnEnable()
    {
        _localizationManager ??= LocalizationManager.Instance;
        TryBindStageBootstrapper();
    }

    private void OnDisable()
    {
        if (_boundToStageBootstrapper && _stageBootstrapper != null)
        {
            _stageBootstrapper.OnStageInitComplete -= Init;
            _boundToStageBootstrapper = false;
        }
    }

    private void Start()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Translation;
        Translation();
    }

    private void Update()
    {
        // HP/EXP는 StageModel 없이도 표시 가능하므로, 스테이지 조립 이벤트만 기다리지 않는다.
        if (_presenter == null)
            TryInitializePresenter(null);

        // StageBootstrapper는 런타임에 생성/조립되어 OnEnable 시점엔 아직 없을 수 있다.
        // 늦게라도 찾아서 구독되면 더 이상 폴링하지 않는다.
        if (!_boundToStageBootstrapper)
            TryBindStageBootstrapper();
        
        // 피드백 바의 깜빡임을 표현하기 위해 시간 주입이 필요하다. 차후 플레이어가 상태이상에 걸린다면 이 통로를 통해 피드백 바를 다른 색으로도 변경 가능
        if(_presenter != null) _presenter.Update(Time.deltaTime);
    }

    // StageBootstrapper를 찾아 OnStageInitComplete를 구독한다.
    // 이미 스테이지가 조립됐으면(캐시된 모델 존재) 즉시 초기화한다 → 씬 배선 없이도 HUD가 동작.
    private void TryBindStageBootstrapper()
    {
        if (_boundToStageBootstrapper) return;   // 이미 구독됨 (멱등 보장 — 중복 구독 방지)

        if (_stageBootstrapper == null) _stageBootstrapper = FindFirstObjectByType<StageBootstrapper>();
        if (_stageBootstrapper == null) return;

        _stageBootstrapper.OnStageInitComplete += Init;
        _boundToStageBootstrapper = true;

        if (_stageBootstrapper.CurrentStageModel != null)
            Init(_stageBootstrapper.CurrentStageModel);
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Translation;
    }

    // ---------- 초기화 ----------
    private void Init(StageModel stageModel)
    {
        TryInitializePresenter(stageModel);
    }

    private bool TryInitializePresenter(StageModel stageModel)
    {
        // 런타임 생성된 모델/시스템을 자가 연결 (씬에 직렬화 참조가 비어 있어도 동작).
        if (_playerModel == null) _playerModel = FindFirstObjectByType<PlayerModel>();
        if (_comboModel == null) _comboModel = FindFirstObjectByType<ComboModel>();
        if (_growthSystem == null) _growthSystem = FindFirstObjectByType<InGameGrowthSystem>();
        if (_loopManager == null) _loopManager = FindFirstObjectByType<StageLoopManager>();

        if (_playerModel == null || _comboModel == null || _growthSystem == null)
        {
            if (!_hasWarnedMissingCoreReferences)
            {
                Debug.LogWarning("[InGameHUDView] PlayerModel 또는 ComboModel 또는 InGameGrowthSystem 참조가 비어 있어 HUD 초기화를 대기합니다.", this);
                _hasWarnedMissingCoreReferences = true;
            }

            return false;
        }

        if (_presenter != null && _stageModel == stageModel)
            return true;

        _presenter?.Dispose();
        _stageModel = stageModel;
        _presenter = new InGameHUDPresenter(this, _playerModel, _comboModel, _growthSystem, _loopManager, _stageModel, _feedBackColor);
        return true;
    }

    // ---------- IInGameHUDView ----------
    public void UpdateHP(float ratio)
    {
        if (_hpSlider != null)
            _hpSlider.value = ratio;
    }

    public void UpdateHPText(int current, int max)
    {
        if(_hpText != null)
            _hpText.text = $"{current.ToString("N0")} / {max.ToString("N0")}";
    }

    public void UpdateEXP(float ratio)
    {
        if (_expSlider != null)
            _expSlider.value = ratio;
    }

    public void UpdateEXPText(int current, int max)
    {
        if(_expText != null)
            _expText.text = $"{current.ToString("N0")} / {max.ToString("N0")}";
    }

    public void UpdateLevelText(int current)
    {
        if (_levelText != null)
            _levelText.text = $"{_currentLevelLabelText} {current}";
    }

    public void UpdateCombo(int count)
    {
        if(count == 0) return;
        
        if (_comboText != null)
        {
            _comboText.text = $"{count} {COMBO_TEXT}";
            OnComboTimerActive?.Invoke();
        }
    }

    public void UpdateStageTimer(float remainingTime)
    {
        if (_stageProgressTimer != null)
        {
            float displayTime = Mathf.Max(0f, remainingTime);
            _stageProgressTimer.text = displayTime.ToString("F2") + "s";
        }
    }

    public void UpdateWaveStateText(StageModel.WaveState waveState)
    {
        if(_waveStateText.text == null) return;
        
        if (waveState == StageModel.WaveState.WaveRunning)
        {
            _waveStateText.text = REMAIN_TIME_TEMP_TEXT;
        }
        else if (waveState == StageModel.WaveState.WaveTransition)
        {
            _waveStateText.text = TRANSITION_TIME_TEMP_TEXT;
        }
    }

    public void UpdateSpecialWavePopup(StageModel.SpawnType spawnType)
    {
        OnSpecialWaveActive?.Invoke(spawnType);
    }

    public void UpdateStageProgress(int chapterOrder, int stageOrder)
    {
        if(_curChapterViwerText != null)
        {
            _curChapterViwerText.text = _localizationManager != null
                ? _localizationManager.Get(12000, LocalizingType.UI, chapterOrder)
                : $"{_currentChapterLabelText} {chapterOrder}";
        }
        
        if(_curStageViwerText != null)
        {
            _curStageViwerText.text = _localizationManager != null
                ? _localizationManager.Get(12001, LocalizingType.UI, stageOrder)
                : $"{_currentStageLabelText} {stageOrder}";
        }
    }

    public void ShowLevelUpEffect() { /* DOTween 연출 넣을 자리 */ }

    private void Translation()
    {
        if (LocalizationManager.Instance == null)
        {
            if(!_translationDictionary.TryGetValue("ko", out var tempData)) 
                return;
            
            _currentChapterLabelText = tempData.TryGetValue(nameof(_currentChapterLabelText), out var chapterLabel)
                ? chapterLabel : string.Empty;
            _currentStageLabelText = tempData.TryGetValue(nameof(_currentStageLabelText), out var stageLabel)
                ? stageLabel : string.Empty;
            _currentLevelLabelText = tempData.TryGetValue(nameof(_currentLevelLabelText), out var levelLabel)
                ? levelLabel : string.Empty;
        }
        else
        {
            if(!_translationDictionary.TryGetValue(LocalizationManager.Instance.CurrentLanguage, out var tempData)) 
                return;
            
            _currentChapterLabelText = tempData.TryGetValue(nameof(_currentChapterLabelText), out var chapterLabel)
                ? chapterLabel : string.Empty;
            _currentStageLabelText = tempData.TryGetValue(nameof(_currentStageLabelText), out var stageLabel)
                ? stageLabel : string.Empty;
            _currentLevelLabelText = tempData.TryGetValue(nameof(_currentLevelLabelText), out var levelLabel)
                ? levelLabel : string.Empty;
        }
    }

    public void SetFeedBackBarColor(Color color)
    {
        _feedbackBar.color = color;
    }
}
