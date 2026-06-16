// 담당자 : 정승우
// 설명   : 인챈트 선택 팝업 View 인터페이스

using System;

public interface IEnchantSelectView
{
    void Show();
    void Hide();
    void SetChoices(Legacy_EnchantDisplayData[] choices);
    void ShowDeleteConfirm(Legacy_EnchantDisplayData toDelete, Legacy_EnchantDisplayData toAcquire);
    event Action<int> OnChoiceSelected;
    event Action OnSkipSelected;
    event Action<int> OnDeleteConfirmed;
    event Action OnRerollSelected;
    void SetRerollAvailable(bool available, int remaining);
}
