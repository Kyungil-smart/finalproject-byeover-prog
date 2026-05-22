// 담당자 : 정승우
// 설명   : 캐릭터 성장 Presenter

public class GrowthPresenter
{
    private readonly IGrowthView _view;
    private readonly OutGameGrowthSystem _growth;
    private readonly CurrencyModel _currency;
    private readonly PlayerProgressModel _progress;
    private readonly ConfigRepo _configRepo;

    public GrowthPresenter(IGrowthView view, OutGameGrowthSystem growth,
        CurrencyModel currency, PlayerProgressModel progress, ConfigRepo configRepo)
    {
        _view = view;
        _growth = growth;
        _currency = currency;
        _progress = progress;
        _configRepo = configRepo;

        _view.OnLevelUpClicked += HandleLevelUp;
        _currency.OnCurrencyChanged += HandleCurrencyChanged;
        _progress.OnCharacterLevelChanged += HandleLevelChanged;
    }

    public void Dispose()
    {
        _view.OnLevelUpClicked -= HandleLevelUp;
        _currency.OnCurrencyChanged -= HandleCurrencyChanged;
        _progress.OnCharacterLevelChanged -= HandleLevelChanged;
    }

    public void Refresh()
    {
        _view.SetCurrentLevel(_progress.CharacterLevel);
        var data = _configRepo.GetOutGrowth(_progress.CharacterLevel);
        if (data != null)
        {
            _view.SetRequiredResources(data.RequiredGold, data.RequiredParchment);
            _view.EnableLevelUpButton(_growth.CanLevelUp());
        }
    }

    private void HandleLevelUp()
    {
        _growth.LevelUp();
        _view.PlayLevelUpEffect();
        Refresh();
    }

    private void HandleCurrencyChanged(int g, int p)
    {
        _view.SetCurrentResources(g, p);
        _view.EnableLevelUpButton(_growth.CanLevelUp());
    }

    private void HandleLevelChanged(int lv) => Refresh();
}
