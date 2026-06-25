using System;
using System.Collections.Generic;
using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 뽑기 결과 확인 후 메인 화면 복귀 시 출력할 '누적 보상' 팝업을 순차(겹침 방지)로 처리하는 큐.
// 누적 보상은 지급 횟수만큼 1페이지씩 순차 출력하며, 각 페이지는 닫힐 때 콜백(ShowNext)으로
// 다음 페이지를 연다 → 동시에 두 팝업이 열리지 않는다.
// (자동 분해 결과는 별도 팝업이 아니라 결과 팝업의 RewardPreviewSlot 에 표시되므로 여기서 다루지 않는다.)
public class ArtifactGachaPopupQueue : MonoBehaviour
{
    [Header("누적 보상 팝업")]
    [SerializeField] private GachaRewardPopup _rewardPopup;

    [Header("보상 아이콘 (선택)")]
    [Tooltip("누적 보상 아이콘(아이콘 매핑 전 임시. 비우면 슬롯은 수량만)")]
    [SerializeField] private Sprite _mileageIcon;

    [Header("문구")]
    [SerializeField] private string _mileageTitleFormat = "누적 보상 ({0}/{1})";

    // 결과 확인 전까지(재추첨 포함) 누적 병합되는 대기 결과.
    private ArtifactGachaResult _pending;
    private readonly Queue<Action> _queue = new Queue<Action>();
    private bool _running;

    public bool HasPending => _pending != null && _pending.HasMileage;

    // 뽑기 처리 결과 중 누적 보상분을 대기열에 병합한다(즉시 출력하지 않음).
    public void Enqueue(ArtifactGachaResult result)
    {
        if (result == null || !result.HasMileage)
            return;

        if (_pending == null)
            _pending = new ArtifactGachaResult();

        _pending.Merge(result);
    }

    // 메인 화면 복귀 시 호출. 대기 중인 누적 보상 페이지를 순차 출력한다.
    public void Flush()
    {
        if (_running || _rewardPopup == null)
            return;

        if (_pending == null || !_pending.HasMileage)
        {
            _pending = null;
            return;
        }

        ArtifactGachaResult data = _pending;
        _pending = null;
        _queue.Clear();

        // 누적 보상 (지급 횟수만큼 1페이지씩 순차 출력 — 합치지 않는다)
        {
            int total = data.MileageRewardCount;
            for (int i = 0; i < total; i++)
            {
                int index = i + 1;
                string title = string.Format(_mileageTitleFormat, index, total);
                var entries = new List<GachaRewardPopup.Entry>
                {
                    new GachaRewardPopup.Entry(_mileageIcon, data.MileageRewardAmount),
                };

                _queue.Enqueue(() => _rewardPopup.Open(title, entries, ShowNext));
            }
        }

        if (_queue.Count == 0)
            return;

        _running = true;
        ShowNext();
    }

    private void ShowNext()
    {
        if (_queue.Count == 0)
        {
            _running = false;
            return;
        }

        Action step = _queue.Dequeue();
        step.Invoke();
    }
}
