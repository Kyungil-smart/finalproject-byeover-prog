// 담당자 : 정승우
// 설명   : 인게임 HUD View -- HP, EXP, 콤보, 진행도 표시

// 수정자 : 정승우
// 수정내용 : Model 참조가 비어 있을 때 Presenter 생성을 건너뛰어 테스트 씬 NullReference 방지

// 수정자 : 김영찬
// 수정내용 : 인게임 UI에 넘겨줄 정보 최신화

using System;
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
    
    // ---------- Private ----------
    private InGameHUDPresenter _presenter;
    private StageModel _stageModel;
    
    // ---------- 이벤트 ----------
    public event Action OnComboTimerActive;
    public event Action<StageModel.SpawnType> OnSpecialWaveActive;
    
    // ---------- Const Text ----------
    private const string CHAPTER_KOR_TEXT = "챕터";
    private const string STAGE_KOR_TEXT = "스테이지";
    private const string CHAPTER_ENG_TEXT = "Chapter";
    private const string STAGE_ENG_TEXT = "Stage";
    
    private const string REMAIN_TIME_KOR_TEXT = "남은 시간";
    private const string REMAIN_TIME_ENG_TEXT = "Remain Time";
    private const string TRANSITION_TIME_KOR_TEXT = "다음 웨이브 대기";
    private const string TRANSITION_TIME_ENG_TEXT = "Transition Time";

    private const string COMBO_TEXT = "COMBO";
    
    private const string LEVEL_KOR_TEXT = "레벨";
    private const string LEVEL_ENG_TEXT = "Level";

    private bool _boundToStageBootstrapper;

    // ---------- 생명주기 ----------
    private void OnEnable()
    {
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

    private void Update()
    {
        // StageBootstrapper는 런타임에 생성/조립되어 OnEnable 시점엔 아직 없을 수 있다.
        // 늦게라도 찾아서 구독되면 더 이상 폴링하지 않는다.
        if (!_boundToStageBootstrapper)
            TryBindStageBootstrapper();
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
    }

    // ---------- 초기화 ----------
    private void Init(StageModel stageModel)
    {
        // 런타임 생성된 모델/시스템을 자가 연결 (씬에 직렬화 참조가 비어 있어도 동작).
        if (_playerModel == null) _playerModel = FindFirstObjectByType<PlayerModel>();
        if (_comboModel == null) _comboModel = FindFirstObjectByType<ComboModel>();
        if (_growthSystem == null) _growthSystem = FindFirstObjectByType<InGameGrowthSystem>();
        if (_loopManager == null) _loopManager = FindFirstObjectByType<StageLoopManager>();

        if (_playerModel == null || _comboModel == null || _growthSystem == null || _loopManager == null)
        {
            Debug.LogWarning("[InGameHUDView] PlayerModel 또는 ComboModel 또는 InGameGrowthSystem 또는 StageLoopManager 참조가 비어 있어 초기화를 건너뜁니다.", this);
            return;
        }

        _presenter?.Dispose();
        _stageModel = stageModel;
        _presenter = new InGameHUDPresenter(this, _playerModel, _comboModel, _growthSystem,  _loopManager,_stageModel);
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
        if(_levelText != null)
            _levelText.text = $"{LEVEL_KOR_TEXT} {current}";
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
            _stageProgressTimer.text = displayTime.ToString("F2");
        }
    }

    public void UpdateWaveStateText(StageModel.WaveState waveState)
    {
        if (waveState == StageModel.WaveState.WaveRunning)
        {
            _waveStateText.text = REMAIN_TIME_KOR_TEXT;
        }
        else if (waveState == StageModel.WaveState.WaveTransition)
        {
            _waveStateText.text = TRANSITION_TIME_KOR_TEXT;
        }
    }

    public void UpdateSpecialWavePopup(StageModel.SpawnType spawnType)
    {
        OnSpecialWaveActive?.Invoke(spawnType);
    }

    public void UpdateStageProgress(int stageId)
    {
        if (_curChapterViwerText != null && _curStageViwerText != null)
        {
            int chapterIndex = stageId / 100;
            int stageIndex = stageId % 100;
            _curChapterViwerText.text = $"{chapterIndex} {CHAPTER_KOR_TEXT}";
            _curStageViwerText.text = $"{stageIndex} {STAGE_KOR_TEXT}";
        }
    }

    public void Translation()
    {
        // ToDO : 차후 번역 나오면 작성
    }

    public void ShowLevelUpEffect() { /* DOTween 연출 넣을 자리 */ }
}
