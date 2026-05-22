// 담당자 : 정승우
// 설명   : 플레이어 진행상황 Model -- 캐릭터 레벨, 스테이지 해금

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 레벨, 현재 챕터/스테이지, 해금 목록을 관리한다.
/// 로비에서 표시하고, 클라우드 저장할 때 여기서 데이터를 가져감.
/// </summary>
public class PlayerProgressModel : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnCharacterLevelChanged;
    public event Action OnProgressUpdated;

    // ---------- 데이터 ----------
    public int CharacterLevel { get; private set; } = 1;
    public int CurrentChapter { get; private set; } = 1;
    public int CurrentStage { get; private set; } = 1;
    public List<int> UnlockedStages { get; private set; } = new List<int>();

    // ---------- 초기화 ----------
    public void Initialize(int charLevel, int chapter, int stage, List<int> unlocked)
    {
        CharacterLevel = charLevel;
        CurrentChapter = chapter;
        CurrentStage = stage;
        UnlockedStages = unlocked ?? new List<int>();
        OnProgressUpdated?.Invoke();
    }

    // ---------- 조작 ----------
    public void SetCharacterLevel(int level)
    {
        CharacterLevel = level;
        OnCharacterLevelChanged?.Invoke(level);
    }

    public void SetCurrentStage(int chapter, int stage)
    {
        CurrentChapter = chapter;
        CurrentStage = stage;
        OnProgressUpdated?.Invoke();
    }

    public void UnlockStage(int stageId)
    {
        if (!UnlockedStages.Contains(stageId))
        {
            UnlockedStages.Add(stageId);
            OnProgressUpdated?.Invoke();
        }
    }

    public bool IsStageUnlocked(int stageId) => UnlockedStages.Contains(stageId);
}