using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 20회 단위 누적(마일리지) 보상 안내 팝업. 한 번에 1회분만 표시하며,
// 누적 보상이 여러 개면 큐가 이 팝업을 닫을 때마다 다시 열어 순차 출력한다.
// 닫기 버튼 → onClosed 콜백으로 큐에 다음 팝업을 알린다.
public class MileageRewardPopup : MonoBehaviour
{
    [Header("팝업 루트(실제 팝업 오브젝트)")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _closeButton;

    [Header("표시 텍스트 (선택)")]
    [Tooltip("예: '누적 보상 (1/3)'")]
    [SerializeField] private TMP_Text _titleText;
    [Tooltip("보상 아이템 ID 표시(현지화/아이콘 연결 전 임시)")]
    [SerializeField] private TMP_Text _rewardItemText;
    [Tooltip("보상 수량 표시")]
    [SerializeField] private TMP_Text _rewardAmountText;

    [SerializeField] private string _titleFormat = "누적 보상 ({0}/{1})";

    private Action _onClosed;
    private bool _bound;

    private void Awake() => Bind();

    private void Bind()
    {
        if (_bound) return;
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleClose); // 중복 등록 방지
            _closeButton.onClick.AddListener(HandleClose);
        }
        _bound = true;
    }

    // index/total 은 '몇 번째 / 전체' 누적 보상인지 표시용.
    public void Open(int rewardItemId, int rewardAmount, int index, int total, Action onClosed)
    {
        Bind();
        _onClosed = onClosed;

        if (_titleText != null) _titleText.text = string.Format(_titleFormat, index, total);
        if (_rewardItemText != null) _rewardItemText.text = $"#{rewardItemId}";
        if (_rewardAmountText != null) _rewardAmountText.text = $"x{rewardAmount}";

        if (_root != null) _root.SetActive(true);
    }

    private void HandleClose()
    {
        if (_root != null) _root.SetActive(false);

        Action cb = _onClosed;
        _onClosed = null;
        cb?.Invoke();
    }
}
