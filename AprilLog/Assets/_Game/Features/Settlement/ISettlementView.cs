// 담당자 : 정승우
// 설명   : 정산 화면 View 인터페이스

using System;

public interface ISettlementView
{
    void Show();
    void Hide();
    void SetResult(bool isVictory);
    void SetStats(int maxCombo, int totalDamage);
    void SetRewards(int gold, int parchment);
    event Action OnConfirmClicked;
}
