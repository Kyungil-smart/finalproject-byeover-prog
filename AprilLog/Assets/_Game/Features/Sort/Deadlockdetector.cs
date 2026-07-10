// 담당자 : 정승우
// 설명   : 데드락 판정만 담당

// 3차 수정자 : 조규민
// 수정 내용 : 퍼플 소트 모든 슬롯 가득 참 조건으로 데드락 팝업 판정 범위 축소

using UnityEngine;

/// <summary>
/// Sort 퍼즐 보드가 완전히 가득 찼는지 판정한다.
/// </summary>
public class DeadlockDetector : MonoBehaviour
{
    public bool IsDeadlock(SortModel model)
    {
        if (model == null)
        {
            return false;
        }

        return model.CountEmptySlots() == 0;
    }
}
