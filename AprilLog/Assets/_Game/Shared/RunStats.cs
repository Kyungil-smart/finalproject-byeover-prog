// 담당자 : 정승우
// 설명   : 한 판(챕터) 동안의 통계 누적 — 정산 화면용. 챕터 시작 시 Reset() 호출.

/// <summary>
/// 인게임 한 판의 통계(총 데미지 등)를 누적한다. 정산에서 읽어 표시.
/// 챕터 시작 시 InGameBootstrap이 Reset()을 호출한다.
/// </summary>
public static class RunStats
{
    public static int TotalDamage { get; private set; }

    public static void Reset()
    {
        TotalDamage = 0;
    }

    public static void AddDamage(int amount)
    {
        if (amount > 0)
            TotalDamage += amount;
    }
}
