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

    private void ApplyTexts() { /* 로컬라이제이션 텍스트 적용 */ }

    public void SetPlayerInfo(string name, int level) => _levelText.SetText("Lv.{0}", level);
    public void SetStageButtons(StageDisplayData[] stages) { /* 스테이지 버튼 생성 */ }
    public void SetCurrency(int gold, int parchment)
    {
        _goldText.SetText("{0}", gold);
        _parchmentText.SetText("{0}", parchment);
    }
    public void ShowResumePrompt() { /* 이어하기 팝업 */ }

    // 버튼 콜백
    public void OnStageButtonClicked(int stageId) => OnStageSelected?.Invoke(stageId);
    public void OnGrowthButtonClicked() => OnGrowthClicked?.Invoke();
    public void OnBookButtonClicked() => OnBookClicked?.Invoke();
    public void OnOptionButtonClicked() => OnOptionClicked?.Invoke();
}
