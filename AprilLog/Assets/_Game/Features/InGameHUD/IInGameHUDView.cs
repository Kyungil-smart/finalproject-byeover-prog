// 담당자 : 정승우
// 설명   : 인게임 HUD View 인터페이스

/// <summary>
/// 인게임 HUD가 구현해야 할 표시 메서드.
/// </summary>
public interface IInGameHUDView
{
    void UpdateHP(float ratio);
    void UpdateShield(float ratio);
    void UpdateEXP(float ratio);
    void UpdateCombo(int count);
    void UpdateComboTimer(float remainRatio);
    void UpdateStageProgress(float ratio);
    void ShowLevelUpEffect();
}
