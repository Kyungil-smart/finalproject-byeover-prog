using System;
using System.Collections.Generic;
using UnityEngine;

// 작성자 : 홍정옥
// 설명 : 뽑기 결과 확인 후 메인 화면 복귀 시 출력할 팝업들을 순차(겹침 방지)로 처리하는 큐
// 출력 순서:
// 1) 자동 분해 결과 팝업(발생 시 1회)
// 2) 누적 보상 팝업(지급 횟수만큼 1개씩 순차)
// 각 팝업은 닫힐 때 콜백으로 다음 팝업을 연다 → 동시에 두 팝업이 열리지 않는다
public class ArtifactGachaPopupQueue : MonoBehaviour
{
    [Header("팝업 참조")]
    [SerializeField] private AutoDecomposeResultPopup _autoDecomposePopup;
    [SerializeField] private MileageRewardPopup _mileagePopup;

    // 결과 확인 전까지(재추첨 포함) 누적 병합되는 대기 결과
    private ArtifactGachaResult _pending;
    private readonly Queue<Action> _queue = new Queue<Action>();
    private bool _running;

    public bool HasPending => _pending != null && _pending.HasAny;

    // 뽑기 처리 결과를 대기열에 병합한다(즉시 출력하지 않음)
    public void Enqueue(ArtifactGachaResult result)
    {
        if (result == null || !result.HasAny)
            return;

        if (_pending == null)
            _pending = new ArtifactGachaResult();

        _pending.Merge(result);
    }

    // 메인 화면 복귀 시 호출
    // 대기 중인 팝업을 순차 출력한다
    public void Flush()
    {
        if (_running)
            return;

        if (_pending == null || !_pending.HasAny)
        {
            _pending = null;
            return;
        }

        ArtifactGachaResult data = _pending;
        _pending = null;
        _queue.Clear();

        // 1)자동 분해 결과 팝업 (발생 시 1회)
        if (data.HasAutoDecompose && _autoDecomposePopup != null)
            _queue.Enqueue(() => _autoDecomposePopup.Open(data, ShowNext));

        // 2)누적 보상 팝업 (지급 횟수만큼 1개씩 순차 출력 — 합치지 않는다)
        if (data.HasMileage && _mileagePopup != null)
        {
            int total = data.MileageRewardCount;
            for (int i = 0; i < total; i++)
            {
                int index = i + 1;
                _queue.Enqueue(() => _mileagePopup.Open(data.MileageRewardItem, data.MileageRewardAmount, index, total, ShowNext));
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
