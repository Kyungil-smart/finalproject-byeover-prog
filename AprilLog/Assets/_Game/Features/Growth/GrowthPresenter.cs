// 담당자 : 정승우
// 설명   : 캐릭터 성장 Presenter

using UnityEngine;

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
        if (view == null || growth == null || currency == null || progress == null || configRepo == null)
        {
            Debug.LogWarning("[GrowthPresenter] Required dependency is missing. Presenter initialization skipped.");
            return;
        }

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
        if (_view == null || _currency == null || _progress == null)
            return;

        _view.OnLevelUpClicked -= HandleLevelUp;
        _currency.OnCurrencyChanged -= HandleCurrencyChanged;
        _progress.OnCharacterLevelChanged -= HandleLevelChanged;
    }

    public void Refresh()
    {
        if (_view == null || _progress == null || _configRepo == null)
            return;

        _view.SetCurrentLevel(_progress.CharacterLevel);
        var data = _configRepo.GetOutLevel(_progress.CharacterLevel);
        if (data != null)
        {
            _view.SetRequiredResources(data.RequiredGold, data.RequiredParchment);
            _view.EnableLevelUpButton(_growth != null && _growth.CanLevelUp());
        }
        else
        {
            _view.SetRequiredResources(0, 0);
            _view.EnableLevelUpButton(false);
        }
    }

    private void HandleLevelUp()
    {
        if (_growth == null || _view == null)
            return;

        _growth.LevelUp();
        _view.PlayLevelUpEffect();
        Refresh();
    }

    private void HandleCurrencyChanged(int g, int p)
    {
        if (_view == null)
            return;

        _view.SetCurrentResources(g, p);
        _view.EnableLevelUpButton(_growth != null && _growth.CanLevelUp());
    }

    private void HandleLevelChanged(int lv) => Refresh();
}
