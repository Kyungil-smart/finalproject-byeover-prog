// 담당자 : 정승우
// 설명   : 캐릭터 성장 View 인터페이스

using System;

public interface IGrowthView
{
    void SetCurrentLevel(int level);
    void SetRequiredResources(int gold, int parchment);
    void SetCurrentResources(int gold, int parchment);
    void EnableLevelUpButton(bool canAfford);
    void PlayLevelUpEffect();
    event Action OnLevelUpClicked;
    event Action OnCloseClicked;
}
