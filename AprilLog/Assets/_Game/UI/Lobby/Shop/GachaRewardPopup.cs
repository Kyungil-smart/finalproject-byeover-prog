using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 누적(마일리지) 보상 안내 팝업. (자동 분해 보상은 결과 팝업의 RewardPreviewSlot 에 표시되므로 별개)
// 제목 + 보상 슬롯(콘텐트에 프리팹 동적 생성)으로 구성하며, 누적 보상이 여러 개면 큐가 같은 팝업을
// 데이터만 바꿔 1페이지씩 재사용해 순차 출력한다. 닫히면 콜백으로 다음 페이지를 띄움.
public class GachaRewardPopup : MonoBehaviour
{
    // 보상 한 줄(아이콘 + 수량). 콘텐트에 RewardPreviewSlotView 로 생성된다.
    public struct Entry
    {
        public Sprite Icon;
        public int Amount;
        public Entry(Sprite icon, int amount) { Icon = icon; Amount = amount; }
    }

    [Header("팝업 루트(실제 팝업 오브젝트)")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _titleText;

    [Header("콘텐트(보상 슬롯 동적 생성)")]
    [Tooltip("보상 슬롯이 생성될 부모 Transform")]
    [SerializeField] private Transform _content;
    [Tooltip("보상 슬롯 프리팹(RewardPreviewSlotView 부착 — 분해 미리보기 슬롯 재사용 가능)")]
    [SerializeField] private RewardPreviewSlotView _entryPrefab;

    private readonly List<RewardPreviewSlotView> _spawned = new List<RewardPreviewSlotView>();
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

    // 제목 + 보상 목록을 채우고 팝업을 연다. 닫으면 onClosed 가 호출된다.
    public void Open(string title, IList<Entry> entries, Action onClosed)
    {
        Bind();
        _onClosed = onClosed;

        if (_titleText != null) _titleText.text = title;

        BuildEntries(entries);

        if (_root != null) _root.SetActive(true);
    }

    private void BuildEntries(IList<Entry> entries)
    {
        ClearEntries();
        if (_content == null || _entryPrefab == null || entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e.Amount <= 0) continue; // 값 있는 보상만 그린다(분해 미리보기와 동일 규칙)

            RewardPreviewSlotView slot = Instantiate(_entryPrefab, _content);
            slot.SetReward(e.Icon, e.Amount);
            _spawned.Add(slot);
        }
    }

    private void ClearEntries()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
        _spawned.Clear();
    }

    private void HandleClose()
    {
        if (_root != null) _root.SetActive(false);
        ClearEntries();

        Action cb = _onClosed;
        _onClosed = null;
        cb?.Invoke();
    }
}
