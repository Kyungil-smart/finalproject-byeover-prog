// 담당자 : 정승우
// 설명   : 인챈트 도감 Presenter

using System.Collections.Generic;

public class BookPresenter
{
    private readonly IBookView _view;
    private readonly EnchantBookModel _model;
    private BookFilter _currentFilter = BookFilter.All;

    public BookPresenter(IBookView view, EnchantBookModel model)
    {
        _view = view;
        _model = model;

        _model.OnBookUpdated += HandleBookUpdated;
        _view.OnFilterChanged += HandleFilterChanged;
        _view.OnEnchantClicked += HandleEnchantClicked;
    }

    public void Dispose()
    {
        _model.OnBookUpdated -= HandleBookUpdated;
        _view.OnFilterChanged -= HandleFilterChanged;
        _view.OnEnchantClicked -= HandleEnchantClicked;
    }

    private void HandleBookUpdated()
    {
        var list = _model.GetFiltered(_currentFilter);
        _view.SetEnchantList(list.ToArray());
    }

    private void HandleFilterChanged(BookFilter filter)
    {
        _currentFilter = filter;
        _view.SetFilter(filter);
        HandleBookUpdated();
    }

    private void HandleEnchantClicked(int enchantId)
    {
        var list = _model.GetFiltered(BookFilter.All);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].EnchantId == enchantId)
            {
                _view.ShowEnchantDetail(list[i]);
                break;
            }
        }
    }
}
