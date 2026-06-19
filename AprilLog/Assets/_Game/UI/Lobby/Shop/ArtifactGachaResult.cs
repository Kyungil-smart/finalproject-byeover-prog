// 작성자 : 홍정옥
// 설명   : 한 번의 뽑기(다중 포함) 처리 결과. 메인 화면 복귀 시 팝업 출력에 사용한다.
//          자동 분해 합산 + 누적(마일리지) 보상 지급 건수를 보관한다.
public class ArtifactGachaResult
{
    // ----- 자동 분해(최대 보유 초과분) -----
    public int RareDecomposed;
    public int EpicDecomposed;
    public int LegendaryDecomposed;
    public int TotalStone; // 자동 분해로 획득한 강화석 총량
    public int TotalShard; // 자동 분해로 획득한 레전더리 조각 총량

    // ----- 누적(마일리지) 보상 -----
    public int MileageRewardCount;  // 이번 뽑기에서 새로 통과한 20회 구간 수 (= 팝업 출력 횟수)
    public int MileageRewardItem;   // 누적 보상 1회의 아이템 ID (데이터)
    public int MileageRewardAmount; // 누적 보상 1회의 수량 (데이터)

    public bool HasAutoDecompose =>
        RareDecomposed > 0 || EpicDecomposed > 0 || LegendaryDecomposed > 0;

    public bool HasMileage => MileageRewardCount > 0;

    public bool HasAny => HasAutoDecompose || HasMileage;

    // 여러 번의 재추첨(결과창의 1회/10회 더)을 확인 전까지 누적하기 위한 병합.
    public void Merge(ArtifactGachaResult other)
    {
        if (other == null) return;

        RareDecomposed += other.RareDecomposed;
        EpicDecomposed += other.EpicDecomposed;
        LegendaryDecomposed += other.LegendaryDecomposed;
        TotalStone += other.TotalStone;
        TotalShard += other.TotalShard;

        MileageRewardCount += other.MileageRewardCount;
        // 마일리지 보상 종류/수량은 동일 박스 내에서 같으므로 마지막 값을 유지한다.
        if (other.MileageRewardItem != 0) MileageRewardItem = other.MileageRewardItem;
        if (other.MileageRewardAmount != 0) MileageRewardAmount = other.MileageRewardAmount;
    }
}
