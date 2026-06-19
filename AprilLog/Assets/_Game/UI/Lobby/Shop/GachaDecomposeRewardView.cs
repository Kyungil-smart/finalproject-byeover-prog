using UnityEngine;

// 작성자 : 홍정옥
// 설명 : 가챠 결과 팝업 안에 RewardPreviewSlot 들에 이번 뽑기의 자동 분해 보상(강화석/조각)을 표시한다.
// - 보상 종류 수만큼만 슬롯을 켜고, 남는 슬롯과 보상이 전혀 없을 때는 모두 끈다.
public class GachaDecomposeRewardView : MonoBehaviour
{
    [Header("보상 슬롯")]
    [SerializeField] private RewardPreviewSlotView[] _slots;

    [Header("보상 아이콘")]
    [SerializeField] private Sprite _stoneIcon; // 강화석(레어/에픽 자동 분해)
    [SerializeField] private Sprite _shardIcon; // 레전더리 조각(레전더리 자동 분해)

    // 자동 분해 보상 표시. 보상이 없으면 모든 슬롯을 끈다.
    public void Show(int stone, int shard)
    {
        if (_slots == null)
            return;

        int used = 0;
        used = ApplyOne(used, _stoneIcon, stone);
        used = ApplyOne(used, _shardIcon, shard);

        // 남는 슬롯은 끈다.
        for (int i = used; i < _slots.Length; i++)
            if (_slots[i] != null) _slots[i].gameObject.SetActive(false);
    }

    // 모든 슬롯 끄기(보상 없음과 동일).
    public void Clear() => Show(0, 0);

    private int ApplyOne(int index, Sprite icon, int amount)
    {
        if (amount <= 0 || index >= _slots.Length)
            return index;

        RewardPreviewSlotView slot = _slots[index];
        if (slot != null)
        {
            slot.gameObject.SetActive(true);
            slot.SetReward(icon, amount);
        }
        return index + 1;
    }
}
