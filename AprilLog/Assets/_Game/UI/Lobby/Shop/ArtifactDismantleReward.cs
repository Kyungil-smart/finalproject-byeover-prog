using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 아티팩트 분해 보상(강화석/레전더리 조각)을 등급,개수로 계산하는 공용 헬퍼
//          자동 분해(가챠 초과분)와 예상 보상 미리보기가 동일한 데이터/식을 쓰도록 한 곳에 모은다
public static class ArtifactDismantleReward
{
    public struct Result
    {
        public int Stone; // 강화석
        public int Shard; // 레전더리 조각
    }

    // grade 등급 1개를 분해했을 때의 보상 count 개면 곱한다
    public static Result Calculate(string grade, int count)
    {
        Result result = default;
        if (count <= 0 || string.IsNullOrEmpty(grade))
            return result;

        GearRepo repo = DataManager.Instance != null ? DataManager.Instance.GearRepo : null;
        GearDismantleData data = repo != null ? repo.GetGearDismantleData(grade) : null;
        if (data == null)
        {
            Debug.LogWarning($"[ArtifactDismantleReward] 분해 보상 데이터를 찾을 수 없습니다. Grade: {grade}");
            return result;
        }

        int total = data.RewardAmount * count;

        if (grade == "Legendary")
            result.Shard = total;
        else
            result.Stone = total;

        return result;
    }
}
