// 담당자 : 정승우
// 설명   : 데드락 판정만 담당

using UnityEngine;

/// <summary>
/// 퍼즐 테이블이 데드락 상태인지 판정한다.
/// 빈 칸 없거나, 빈 칸 2개 이하인데 3매칭 불가능하면 데드락.
/// </summary>
public class DeadlockDetector : MonoBehaviour
{
    public bool IsDeadlock(SortModel model)
    {
        int emptySlots = model.CountEmptySlots();

        // 빈 칸 없으면 확정
        if (emptySlots == 0) return true;

        // 빈 칸 2개 이하일 때, 같은 유닛 3개 이상 있는지 확인
        if (emptySlots <= 2)
        {
            var counts = model.CountUnitTypes();
            foreach (var pair in counts)
            {
                if (pair.Value >= 3) return false;  // 아직 매칭 가능
            }
            return true;  // 3개 모을 수 있는 유닛이 없음
        }

        return false;
    }
}