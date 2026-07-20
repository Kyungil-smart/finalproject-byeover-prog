using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 고급상자(레전더리 가챠)의 확정/누적 보상 진행도 통합 표시 뷰.
//   - 확정보상(20Reward)        : 천장(Pity) 진행도   = 누적뽑기 % PityCount / PityCount
//   - 누적보상(CumulativeCompensation) : 마일리지 진행도 = 누적뽑기 % StepSize / StepSize
//       + 다음 마일스톤 보상 아이콘(Reward1/Reward2). 보상이 1종이면 Reward2 끄고 Reward1 중앙 이동.
//   - 각 Count 는 현재값만 갈색으로 강조(TMP 리치텍스트), 최대값은 기본색.
//   - 데이터/진행도는 ArtifactGachaPostProcessor 가 단일 소스. 가챠 전환/뽑기 후 Refresh(id) 호출.
public class GachaProgressView : MonoBehaviour
{
    // 아이템 ID -> 아이콘 매핑(프로젝트에 아이템 아이콘 DB가 없어 인스펙터로 직접 연결).
    [System.Serializable]
    public struct ItemIcon
    {
        public int itemId;
        public Sprite icon;
    }

    [Header("데이터 소스")]
    [Tooltip("진행도/보상 데이터의 단일 소스(천장 카운터·마일리지 보상)")]
    [SerializeField] private ArtifactGachaPostProcessor _postProcessor;
    [Tooltip("표시 대상 가챠 ID(고급상자=레전더리). 가챠 전환 시 Refresh(id)로 바뀐다.")]
    [SerializeField] private int _gachaId = 3;

    [Header("확정보상(20Reward) Count")]
    [Tooltip("천장 진행 카운트 텍스트(현재/최대)")]
    [SerializeField] private TMP_Text _confirmedCountText;
    [Tooltip("천장 가챠가 아닐 때 숨길 루트(선택)")]
    [SerializeField] private GameObject _confirmedRoot;

    [Header("누적보상(CumulativeCompensation) Count")]
    [Tooltip("마일리지 진행 카운트 텍스트(현재/최대)")]
    [SerializeField] private TMP_Text _cumulativeCountText;
    [Tooltip("마일리지 가챠가 아닐 때 숨길 루트(선택)")]
    [SerializeField] private GameObject _cumulativeRoot;

    [Header("누적보상 다음 보상 아이콘")]
    [Tooltip("첫 번째 보상 아이콘 Image(Reward1)")]
    [SerializeField] private Image _reward1Icon;
    [Tooltip("두 번째 보상 아이콘 Image(Reward2)")]
    [SerializeField] private Image _reward2Icon;
    [Tooltip("두 번째 보상 루트 오브젝트(프레임 포함). 보상 1종일 때 통째로 끈다. 비우면 아이콘만 끔")]
    [SerializeField] private GameObject _reward2Root;
    [Tooltip("Reward1 의 RectTransform(1종일 때 중앙 이동용)")]
    [SerializeField] private RectTransform _reward1Rect;
    [Tooltip("보상 2종일 때 Reward1 위치(좌측)")]
    [SerializeField] private Vector2 _reward1PosWhenTwo;
    [Tooltip("보상 1종일 때 Reward1 위치(중앙)")]
    [SerializeField] private Vector2 _reward1PosWhenOne;

    [Header("아이템 아이콘 매핑 (itemId → Sprite)")]
    [SerializeField] private List<ItemIcon> _itemIcons = new List<ItemIcon>();

    [Header("강조 표시")]
    [Tooltip("현재 카운트 숫자 강조 색상(갈색 계열)")]
    [SerializeField] private string _currentNumberColorHex = "#8B5A2B";
    [Tooltip("카운트 표기 형식 {0}=색상 {1}=현재값 {2}=최대값")]
    [SerializeField] private string _countFormat = "<color={0}>{1}</color>/{2}";

    public void Refresh() => Refresh(_gachaId);

    // 표시 대상 가챠를 바꾸고 갱신. (ShopGachaPresenter.SetActiveGacha 와 같은 시점에 호출)
    public void Refresh(int gachaId)
    {
        _gachaId = gachaId;
        RefreshConfirmed(gachaId);
        RefreshCumulative(gachaId);
    }

    private void OnEnable() => Refresh();

    // ---------- 확정보상(천장) ----------
    private void RefreshConfirmed(int gachaId)
    {
        int max = _postProcessor != null ? _postProcessor.GetPityMax(gachaId) : -1;
        bool show = max > 0; // 천장 가챠일 때만 표시

        if (_confirmedRoot != null) _confirmedRoot.SetActive(show);

        if (_confirmedCountText != null)
        {
            int current = show ? _postProcessor.GetPityCurrent(gachaId) : 0;
            _confirmedCountText.text = show ? FormatCount(current, max) : string.Empty;
        }
    }

    // ---------- 누적보상(마일리지) ----------
    private void RefreshCumulative(int gachaId)
    {
        // 표기 : "현재 누적 / 다음 마일스톤"(예 25/40). 분모(40)가 '몇 회차 보상'인지 알려준다.
        int current = _postProcessor != null ? _postProcessor.GetMileageCycleProgress(gachaId) : -1;
        int milestone = _postProcessor != null ? _postProcessor.GetNextMilestoneCount(gachaId) : -1;
        bool show = current >= 0 && milestone > 0; // 마일리지 가챠일 때만 표시

        if (_cumulativeRoot != null) _cumulativeRoot.SetActive(show);

        if (_cumulativeCountText != null)
            _cumulativeCountText.text = show ? FormatCount(current, milestone) : string.Empty;

        if (show)
            RefreshNextRewardIcons(gachaId);
        else
            ApplyRewardIcons(0, 0);
    }

    // 다음 마일스톤 보상 아이콘(Reward1/Reward2)을 갱신한다.
    private void RefreshNextRewardIcons(int gachaId)
    {
        GachaRewardData reward = _postProcessor != null ? _postProcessor.GetNextMilestoneReward(gachaId) : null;
        if (reward == null)
        {
            ApplyRewardIcons(0, 0);
            return;
        }

        // 수량이 있는 보상만 '유효 보상 종류'로 본다.
        int first = reward.FirstRewardAmount > 0 ? reward.FirstRewardItem : 0;
        int second = reward.SecondRewardAmount > 0 ? reward.SecondRewardItem : 0;
        ApplyRewardIcons(first, second);
    }

    // first/second = 아이템 ID(0 이면 없음). 1종이면 Reward2 끄고 Reward1 중앙으로.
    private void ApplyRewardIcons(int firstItemId, int secondItemId)
    {
        bool hasFirst = firstItemId != 0;
        bool hasSecond = secondItemId != 0;

        if (_reward1Icon != null)
        {
            _reward1Icon.gameObject.SetActive(hasFirst);
            if (hasFirst) SetIcon(_reward1Icon, firstItemId);
        }

        // 두 번째 보상은 프레임까지 통째로 껐다 켠다(1종일 때 빈 프레임이 남지 않게).
        if (_reward2Root != null)
            _reward2Root.SetActive(hasSecond);
        else if (_reward2Icon != null)
            _reward2Icon.gameObject.SetActive(hasSecond);

        if (_reward2Icon != null && hasSecond)
            SetIcon(_reward2Icon, secondItemId);

        // 보상 1종이면 Reward1 을 중앙으로, 2종이면 좌측 기본 위치로.
        if (_reward1Rect != null && hasFirst)
            _reward1Rect.anchoredPosition = hasSecond ? _reward1PosWhenTwo : _reward1PosWhenOne;
    }

    private void SetIcon(Image image, int itemId)
    {
        Sprite sprite = ResolveIcon(itemId);
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private Sprite ResolveIcon(int itemId)
    {
        for (int i = 0; i < _itemIcons.Count; i++)
        {
            if (_itemIcons[i].itemId == itemId)
                return _itemIcons[i].icon;
        }

        Debug.LogWarning($"[GachaProgressView] 아이템 ID {itemId} 의 아이콘 매핑이 없습니다.", this);
        return null;
    }

    // "<color=#hex>현재</color>/최대" — 현재값만 갈색 강조.
    private string FormatCount(int current, int max)
    {
        return string.Format(_countFormat, _currentNumberColorHex, current, max);
    }
}
