// 담당자 : 정승우
// 설명   : 인챈트 도감 Presenter

using System.Collections.Generic;
using UnityEngine;

public class BookPresenter
{
    private readonly IBookView _view;
    private readonly EnchantBookModel _model;
    private BookFilter _currentFilter = BookFilter.All;

    public BookPresenter(IBookView view, EnchantBookModel model)
    {
        if (view == null || model == null)
        {
            Debug.LogWarning("[BookPresenter] Required dependency is missing. Presenter initialization skipped.");
            return;
        }

        _view = view;
        _model = model;

        _model.OnBookUpdated += HandleBookUpdated;
        _view.OnFilterChanged += HandleFilterChanged;
        _view.OnEnchantClicked += HandleEnchantClicked;
    }

    public void Dispose()
    {
        if (_model == null || _view == null)
            return;

        _model.OnBookUpdated -= HandleBookUpdated;
        _view.OnFilterChanged -= HandleFilterChanged;
        _view.OnEnchantClicked -= HandleEnchantClicked;
    }

    private void HandleBookUpdated()
    {
        if (_model == null || _view == null)
            return;

        var list = _model.GetFiltered(_currentFilter);
        if (list == null)
        {
            _view.SetEnchantList(new EnchantBookDisplayData[0]);
            return;
        }

        _view.SetEnchantList(list.ToArray());
    }

    private void HandleFilterChanged(BookFilter filter)
    {
        if (_view == null)
            return;

        _currentFilter = filter;
        _view.SetFilter(filter);
        HandleBookUpdated();
    }

    private void HandleEnchantClicked(int enchantId)
    {
        if (_model == null || _view == null)
            return;

        var list = _model.GetFiltered(BookFilter.All);
        if (list == null)
            return;

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
