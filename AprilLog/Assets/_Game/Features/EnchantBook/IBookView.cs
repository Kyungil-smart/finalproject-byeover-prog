// 담당자 : 정승우
// 설명   : 인챈트 도감 View 인터페이스

using System;

public interface IBookView
{
    void SetEnchantList(EnchantBookDisplayData[] enchants);
    void SetFilter(BookFilter filter);
    void ShowEnchantDetail(EnchantBookDisplayData data);
    event Action<BookFilter> OnFilterChanged;
    event Action<int> OnEnchantClicked;
    event Action OnCloseClicked;
}
