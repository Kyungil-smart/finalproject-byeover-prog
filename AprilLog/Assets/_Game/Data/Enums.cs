// 담당자 : 정승우
// 설명   : 게임 전체에서 쓰는 Enum 모음

/// <summary>
/// 게임 전체에서 사용하는 열거형을 한 파일에 모아둔다.
/// </summary>

// Sort 유닛 종류 (기획서 기준 5종)
public enum UnitType
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    None        // 자동공격 등에서 특정 유닛이 아닌 경우
}

// 공격 발동 경로
public enum AttackType
{
    Sort,       // 정렬 성공 시
    Combi,      // 조합식 완성 시
    Combo,      // 콤보 배수 도달 시
    Auto        // 인챈트 이후 자동공격
}

// 투사체 타격 방식 (기획서 1-3)
public enum HitRangeType
{
    Single,     // 투사체 -- 직선 이동, 피격 시 소멸
    Piercing,   // 관통 -- 피격해도 안 사라지고 끝까지 감
    Homing,     // 추적기 -- 타겟 따라감, 피격 시 소멸
    Bouncing,   // 튕기기 -- 한 적 치고 가장 가까운 다음 적으로
    Fan,        // 부채꼴 범위
    Circle,     // 원형 범위
    UserTarget  // 유저 스킬 타겟팅 (구슬 드래그)
}

// 스킬 이펙트 종류 (기획서 EffectTable)
public enum StatusEffectType
{
    None,
    Knockback,
    DotDamage,
    Bounce,
    Penetration,
    Targeting
}

// 몬스터 종류 (기획서 6-2)
public enum MonsterType
{
    Normal,     // 일반형 -- 밸런스
    Fast,       // 민첩형 -- 체력 낮고 빠름
    Tank,       // 탱커형 -- 체력 높고 느림
    Gimmick,    // 기믹형 -- 거미줄/돌멩이
    Ranged,     // 원거리형 -- 사거리 길고 체력 적음
    Suicide,    // 자폭형 -- 체력 매우 낮고 매우 빠름
    Elite,      // 엘리트/보스
    Boss
}

// 몬스터 이동 패턴 (기획서 6-1)
public enum MovementPatternType
{
    Straight,   // 일자형 -- x값 유지하면서 내려옴
    Zigzag      // 좌우반복형 -- 정해진 값만큼 좌우 이동
}

// 인챈트 분류
public enum EnchantCategory
{
    SkillNormal,    // 일반 스킬 인챈트
    SkillCombi,     // 조합 스킬 인챈트
    SkillCombo,     // 콤보 스킬 인챈트
    Stat            // 스탯 인챈트
}

// 대기열 조합 난이도 (Sort 알고리즘)
public enum WaitingDifficulty
{
    Low,        // (A,_,_) or (A,A,_) -- 같은 종류 or 1개
    Warning,    // (A,A,B) or (A,B,_) -- 섞인 조합
    High        // (A,B,C) -- 전부 다름
}

// 퍼즐 테이블 난이도 (빈 슬롯 기준)
public enum PuzzleDifficulty
{
    Safe,       // 빈 칸 12개 이상 = 20점
    Normal,     // 빈 칸 5~11개 = 40점
    Danger,     // 빈 칸 2~4개 = 70점
    Critical,   // 빈 칸 1개 = 90점
    Deadlock    // 빈 칸 0개 = 100점
}

// 캐릭터 타입 (능력치 테이블 구분용)
public enum CharacterType
{
    Main,       // 주인공
    Guide,      // 가이드 NPC
    Monster     // 몬스터
}

// 게임 상태 (GameManager FSM)
public enum GameState
{
    Boot,
    Login,
    Lobby,
    InGame,
    Settlement
}

// 인챈트 도감 필터
public enum BookFilter
{
    All,
    Owned,
    NotOwned
}

// 슬롯 상태 (기믹 포함)
public enum SlotState
{
    Empty,      // 비어있음
    Occupied,   // 유닛이 있음
    Locked      // 거미줄로 잠김
}

public enum DamageGroupType
{
    None,
    Fire,
    Ice,
}

public enum LocalizingType
{
    Enchant,
    Gear,
    UI
}
