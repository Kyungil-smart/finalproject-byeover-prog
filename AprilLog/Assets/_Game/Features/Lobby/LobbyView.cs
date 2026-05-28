// 담당자 : 정승우
// 설명   : 로비 View -- 스테이지 선택, 캐릭터 정보 표시

using System;
using UnityEngine;
using TMPro;

public class LobbyView : MonoBehaviour, ILobbyView
{
    public event Action<int> OnStageSelected;
    public event Action OnResumeClicked;
    public event Action OnGrowthClicked;
    public event Action OnBookClicked;
    public event Action OnOptionClicked;

    [Header("참조")]
    [SerializeField] private PlayerProgressModel _progress;
    [SerializeField] private CurrencyModel _currency;
    [SerializeField] private LocalizationManager _localization;

    [Header("UI")]
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _parchmentText;

    private LobbyPresenter _presenter;

    private void Awake()
    {
        if (_progress == null || _currency == null)
        {
            Debug.LogWarning("[LobbyView] Required model is missing. LobbyPresenter creation skipped.");
            return;
        }

        _presenter = new LobbyPresenter(this, _progress, _currency);
    }

    private void Start()
    {
        ApplyTexts();
        if (_localization != null)
            _localization.OnLanguageChanged += ApplyTexts;
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
        if (_localization != null)
            _localization.OnLanguageChanged -= ApplyTexts;
    }

    private void ApplyTexts() { }

    public void SetPlayerInfo(string name, int level)
    {
        if (_levelText != null)
            _levelText.SetText("Lv.{0}", level);
    }

    public void SetStageButtons(StageDisplayData[] stages) { }
    public void SetCurrency(int gold, int parchment)
    {
        if (_goldText != null)
            _goldText.SetText("{0}", gold);

        if (_parchmentText != null)
            _parchmentText.SetText("{0}", parchment);
    }
    public void ShowResumePrompt() { OnResumeClicked?.Invoke(); }

    public void OnStageButtonClicked(int stageId) => OnStageSelected?.Invoke(stageId);
    public void OnGrowthButtonClicked() => OnGrowthClicked?.Invoke();
    public void OnBookButtonClicked() => OnBookClicked?.Invoke();
    public void OnOptionButtonClicked() => OnOptionClicked?.Invoke();
}
