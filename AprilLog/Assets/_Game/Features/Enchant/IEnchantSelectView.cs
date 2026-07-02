// 담당자 : 정승우
// 설명   : 인챈트 선택 팝업 View 인터페이스

// 수정자 : 조규민
// 수정 내용 : 인챈트 카드별 리롤 요청 이벤트 추가

using System;

public interface IEnchantSelectView
{
    void Show();
    void Hide();
    void SetChoices(EnchantDisplayData[] choices);
    void SetCardRerollAvailable(bool[] availableByIndex);
    event Action<int> OnChoiceSelected;
    event Action OnSkipSelected;
    event Action<int> OnDeleteConfirmed;
    event Action OnRerollSelected;
    event Action<int> OnCardRerollSelected;
    void SetRerollAvailable(bool available, int remaining);
}
