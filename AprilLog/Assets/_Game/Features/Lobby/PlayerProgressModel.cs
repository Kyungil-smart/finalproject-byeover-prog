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
    public const int StartLevel = 1;

    // 데이터 미준비(테스트 씬 등) 시 사용할 폴백 상한
    private const int FallbackMaxLevel = 10;

    // 최대 레벨은 OutLevel 테이블 행 수에서 읽는다. 데이터가 없으면 폴백값 사용.
    public static int MaxLevel
    {
        get
        {
            int dataMax = DataManager.Instance != null && DataManager.Instance.ConfigRepo != null
                ? DataManager.Instance.ConfigRepo.GetMaxOutLevel()
                : 0;
            return dataMax > 0 ? dataMax : FallbackMaxLevel;
        }
    }

    // ---------- 이벤트 ----------
    public event Action<int> OnCharacterLevelChanged;
    public event Action OnProgressUpdated;

    // ---------- 데이터 ----------
    public int CharacterLevel { get; private set; } = StartLevel;
    public int CurrentChapter { get; private set; } = 1;
    public int CurrentStage { get; private set; } = 1;
    public List<int> UnlockedStages { get; private set; } = new List<int>();

    // ---------- 초기화 ----------
    public void Initialize(int charLevel, int chapter, int stage, List<int> unlocked)
    {
        SetCharacterLevel(charLevel);
        CurrentChapter = chapter;
        CurrentStage = stage;
        UnlockedStages = unlocked ?? new List<int>();
        OnProgressUpdated?.Invoke();
    }

    // ---------- 조작 ----------
    public void SetCharacterLevel(int level)
    {
        CharacterLevel = Mathf.Clamp(level, StartLevel, MaxLevel);
        OnCharacterLevelChanged?.Invoke(CharacterLevel);
    }

    public bool IsMaxCharacterLevel => CharacterLevel >= MaxLevel;

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
