// 담당자 : 정승우
// 설명   : 인게임 HUD View 인터페이스

// 수정자 : 김영찬
// 수정내용 : 인게임 UI에 넘겨줄 정보 최신화

using UnityEngine;

/// <summary>
/// 인게임 HUD가 구현해야 할 표시 메서드.
/// </summary>
public interface IInGameHUDView
{
    void UpdateHP(float ratio);
    void UpdateHPText(int current, int max);
    void UpdateEXP(float ratio);
    void UpdateEXPText(int current, int max);
    void UpdateLevelText(int current);
    void UpdateCombo(int count);
    void UpdateStageTimer(float remainingTime);
    void UpdateStageProgress(int stageId);
    void UpdateWaveStateText(StageModel.WaveState waveState);
    void UpdateSpecialWavePopup(StageModel.SpawnType spawnType);
    void ShowLevelUpEffect();
    void SetFeedBackBarColor(Color color);
}
