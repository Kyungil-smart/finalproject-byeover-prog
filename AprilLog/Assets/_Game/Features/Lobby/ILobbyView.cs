// 담당자 : 정승우
// 설명   : 로비 View 인터페이스

using System;

public interface ILobbyView
{
    void SetPlayerInfo(string name, int level);
    void SetStageButtons(Legacy_StageDisplayData[] stages);
    void SetCurrency(int gold, int parchment);
    void ShowResumePrompt();
    event Action<int> OnStageSelected;
    event Action OnResumeClicked;
    event Action OnGrowthClicked;
    event Action OnBookClicked;
    event Action OnOptionClicked;
}
