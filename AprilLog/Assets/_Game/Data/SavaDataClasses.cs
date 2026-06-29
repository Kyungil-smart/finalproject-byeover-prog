// 생성자 : 김영찬
// 설명 : 게임 내 모든 로컬/클라우드 세이브 데이터를 정의하는 DTO(Data Transfer Object) 클래스 모음
// 주의 : 이 파일 안의 클래스들은 오직 '데이터 보관' 용도이므로 함수(메서드)를 넣지 마세요!

using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

#region 인챈트 시스템 세이브 데이터

/// <summary>
/// 유저가 획득한 스킬/스탯 인챈트 저장용 구조체<br/>
/// (기존 Legacy_AcquiredEnchant 였으나, 정식 세이브 데이터로 승격됨)<br/>
/// 최종 수정일 : 26.06.12
/// </summary>
[Serializable]
public class AcquiredEnchantSaveData
{
    public int EnchantId;   // Name_ID (스킬) 또는 StatName_ID (스탯)
    public int Level;       // 현재 레벨
}

#endregion

#region 인게임 세이브 데이터

/// <summary>
/// 인게임 세이브 데이터<br/>
/// (기존 Legacy_InGameSaveData 였으나, 정식 세이브 데이터로 승격됨)<br/>
/// 최종 수정일 : 26.06.29
/// </summary>
[Serializable]
public class InGameSaveData
{
    // 스테이지
    public int chapterId;
    public int clearedStage;

    // 플레이어
    public int playerHP;
    public int currentEXP;
    public int inGameLevel;
    
    // 퍼즐
    public int[] puzzleSlots;
    public int[] waitingSlots;
    public int jokerCount; 
    public float jokerRemainingCooldown;
    public int nextStageSeed;
    
    // 인첸트
    public List<AcquiredEnchantSaveData> acquiredEnchants;
    
    // 기록
    public int totalDamage;
    public int highestDamage;
    public Dictionary<int, int> MaxBySkill;
    public int maxCombo;
}

#endregion