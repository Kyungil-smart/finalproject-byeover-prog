// 담당자 : 정승우
// 설명   : 인챈트 도감 View

using System;
using UnityEngine;

public class BookView : MonoBehaviour, IBookView
{
    public event Action<BookFilter> OnFilterChanged;
    public event Action<int> OnEnchantClicked;
    public event Action OnCloseClicked;

    [Header("참조")]
    [SerializeField] private EnchantBookModel _bookModel;

    private BookPresenter _presenter;
    private bool _isInitialized;

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            _presenter = new BookPresenter(this, _bookModel);
        }
        _bookModel.RefreshEntries();
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void SetEnchantList(EnchantBookDisplayData[] enchants) { }
    public void SetFilter(BookFilter filter) { }
    public void ShowEnchantDetail(EnchantBookDisplayData data) { }

    public void OnFilterButtonClicked(int filterIdx) => OnFilterChanged?.Invoke((BookFilter)filterIdx);
    public void OnItemClicked(int enchantId) => OnEnchantClicked?.Invoke(enchantId);
    public void OnCloseButtonClicked() => OnCloseClicked?.Invoke();
}