// 담당자 : 정승우
// 설명   : 인챈트 선택 Presenter

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

using System.Collections.Generic;

/// <summary>
/// 인챈트 선택지 생성 + 유저 선택 처리.
/// </summary>
public class EnchantSelectPresenter
{
    private readonly IEnchantSelectView _view;
    private readonly EnchantModel _model;
    private readonly Legacy_CharacterRepo _repo;
    private readonly ScreenNavigator _navigator;
    private EnchantSelectionLogic _selectionLogic;
    private List<Legacy_EnchantMasterData> _currentChoices;

    public EnchantSelectPresenter(IEnchantSelectView view, EnchantModel model,
        Legacy_CharacterRepo repo, ScreenNavigator navigator)
    {
        _view = view;
        _model = model;
        _repo = repo;
        _navigator = navigator;
        _selectionLogic = new EnchantSelectionLogic(repo, model, new System.Random());

        _view.OnChoiceSelected += HandleChoice;
        _view.OnSkipSelected += HandleSkip;
    }

    public void Dispose()
    {
        _view.OnChoiceSelected -= HandleChoice;
        _view.OnSkipSelected -= HandleSkip;
    }

    public void ShowSelection()
    {
        _currentChoices = _selectionLogic.GenerateChoices();
        var displayData = new Legacy_EnchantDisplayData[_currentChoices.Count];
        for (int i = 0; i < _currentChoices.Count; i++)
        {
            displayData[i] = new Legacy_EnchantDisplayData
            {
                EnchantId = _currentChoices[i].EnchantID,
                Name = _currentChoices[i].Name,
                Level = _model.GetEnchantLevel(_currentChoices[i].EnchantID) + 1
            };
        }
        _view.SetChoices(displayData);
    }

    private void HandleChoice(int index)
    {
        if (index < 0 || index >= _currentChoices.Count) return;
        _model.AcquireEnchant(_currentChoices[index].EnchantID);
        _navigator.HideEnchantSelection();
    }

    private void HandleSkip()
    {
        _navigator.HideEnchantSelection();
    }
}
