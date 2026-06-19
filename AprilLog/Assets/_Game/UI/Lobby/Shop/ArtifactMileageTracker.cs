using UnityEngine;

// 작성자 : 홍정옥
// 설명 : 가챠 누적 보상(20회 단위) 진행도를 관리한다.
// - 전체 누적 뽑기 횟수와 이미 지급된 보상 단계를 분리해서 PlayerPrefs 에 저장
// - 이번 뽑기 전후의 누적 횟수를 비교해 새로 통과한 20회 구간 수를 계산
// - 보상 지급(상태 저장)과 팝업 출력 시점을 분리하기 위해, 지급 단계는 즉시 영구 저장
public class ArtifactMileageTracker : MonoBehaviour
{
    [Header("누적 보상 구간")]
    [Tooltip("누적 보상 1회를 지급하는 뽑기 간격(기획 : 20회)")]
    [SerializeField] private int _stepSize = 20;

    private const string TotalKeyPrefix = "Artifact_Mileage_Total_";
    private const string RewardedStepKeyPrefix = "Artifact_Mileage_RewardedStep_";

    private string TotalKey(int gachaId) => TotalKeyPrefix + gachaId;
    private string RewardedStepKey(int gachaId) => RewardedStepKeyPrefix + gachaId;

    public int GetTotalDrawCount(int gachaId) => PlayerPrefs.GetInt(TotalKey(gachaId), 0);

    // 이번에 count 회 뽑았을 때 새로 통과한 보상 구간 수를 반환하고, 상태(누적횟수·지급단계)를 즉시 저장한다.
    // 반환값 = 이번 뽑기로 새로 지급 대상이 된 누적 보상 개수
    public int RegisterDraws(int gachaId, int count)
    {
        if (count <= 0 || _stepSize <= 0)
            return 0;

        int prevTotal = PlayerPrefs.GetInt(TotalKey(gachaId), 0);
        int newTotal = prevTotal + count;

        // 이미 지급 처리된 단계(없으면 과거 누적분으로 backfill → 중복 지급 방지).
        int rewardedStep = PlayerPrefs.GetInt(RewardedStepKey(gachaId), prevTotal / _stepSize);
        int currentStep = newTotal / _stepSize;

        int earned = Mathf.Max(0, currentStep - rewardedStep);

        // 보상 '수령 상태'는 실제 뽑기 처리 시점에 즉시 저장한다(앱 종료/팝업 실패에도 중복 지급 방지)
        PlayerPrefs.SetInt(TotalKey(gachaId), newTotal);
        PlayerPrefs.SetInt(RewardedStepKey(gachaId), currentStep);
        PlayerPrefs.Save();

        return earned;
    }
}
