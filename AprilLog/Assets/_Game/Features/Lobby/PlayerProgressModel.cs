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

    // 최대 레벨은 OutLevelTable 데이터가 정본(현행 50). 상수 10("테스트 최대")으로 박혀 있어
    // 데이터가 50레벨로 확장된 뒤에도 10에서 멈추던 문제 수정 — 데이터 행이 늘면 상한도 자동 추종.
    public static int MaxLevel
    {
        get
        {
            if (s_cachedMaxLevel > 0) return s_cachedMaxLevel;

            var repo = DataManager.Instance != null ? DataManager.Instance.ConfigRepo : null;
            int dataMax = repo != null ? repo.GetMaxOutLevel() : 0;
            if (dataMax > 0)
            {
                s_cachedMaxLevel = dataMax;   // 데이터가 준비된 뒤에만 캐시
                return dataMax;
            }
            return FallbackMaxLevel;          // 미로드 시 임시값(캐시 안 함, 다음 호출에 재시도)
        }
    }

    private const int FallbackMaxLevel = 50;
    private static int s_cachedMaxLevel;

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
