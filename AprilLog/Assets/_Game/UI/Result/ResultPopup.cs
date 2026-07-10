// 작성자 : 홍정옥
// 설명 : 게임 오버/클리어 시 뜨는 결산(Result) 팝업 - 결과/기록/인챈트/보상/버튼 표시

// 수정자 : 김영찬
// 설명 : 팝업 개/폐시 ScreenNavigator의 함수를 사용하도록 수정 및 로비로 돌아가는 키 입력에 대한 기능 ScreenNavigator와 통합

// 수정자 : 김영찬
// 설명 : Ingamebootstrap.cs 와의 연결 재 구성

// 3차 수정자 : 조규민
// 수정 내용 : 정산 팝업 TOP3 인챈트 데미지에 스킬 아이콘을 함께 표시하고 보상 표시 갱신 안정화

// 4차 수정자 : 김영찬
// 수정 내용 : 이제 씬 전환(재시작 포함)은 InGameNextSceneLoader.cs에서 일괄 담당 -> 이벤트 발송만 남겨둠
//           클리어 시 초회 클리어 시나리오를 보지 않았다면 로비로 돌아가기 이외의 버튼을 비활성화 하는 기능 추가

// 5차 수정자 : 조규민
// 수정 내용 : 정산 보상 슬롯을 실제 지급된 초회/반복 재화 내역 기준으로 표시하고 보상판 높이를 지급 항목 수에 맞게 조절

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 추가: 조규민 - Result 보상 표시 대상에 다이아를 포함한다.
public readonly struct ResultEnchantEntry
{
    public readonly int _skillId;
    public readonly long _damage;

    public ResultEnchantEntry(int _skillId, long _damage)
    {
        this._skillId = _skillId;
        this._damage = _damage;
    }
}

public class ResultPopup : MonoBehaviour
{
    private const int GoldId = 70001;
    private const int ParchmentId = 70002;
    private const int DiamondId = 70003;
    private const int MaxRewardSlotCount = 6;
    private const int CompactRewardSlotThreshold = 3;

    // ---------- 이벤트 (필요 시 외부 구독) ----------
    public event Action OnRetryClicked;
    public event Action OnNextChapterClicked;

    // ---------- 참조 ----------
    [Header("참조")] 
    [SerializeField] private ScreenNavigator _navigator;

    // ---------- Header : 오버/클리어 ----------
    [Header("Header")]
    [SerializeField] private TMP_Text _headerText;
    [SerializeField] private string _clearText = "STAGE CLEAR";
    [SerializeField] private string _overText  = "GAME OVER";

    // ---------- Record : 최고 콤보 / 최고 데미지 ----------
    [Header("Record")]
    [SerializeField] private TMP_Text _maxComboText;
    [SerializeField] private TMP_Text _maxDamageText;

    // ---------- Enchant : 인챈트 3종이 가한 데미지 ----------
    [Header("Enchant (최대 데미지 1,2,3 등)")]
    [Tooltip("1등 스킬 인첸트 데미지")]
    [SerializeField] private TMP_Text _enchantDamage1;
    [Tooltip("1등 스킬 인첸트 이미지")]
    [SerializeField] private Image _enchantImage1;
    [Tooltip("2등 스킬 인첸트 데미지")]
    [SerializeField] private TMP_Text _enchantDamage2;
    [Tooltip("2등 스킬 인첸트 이미지")]
    [SerializeField] private Image _enchantImage2;
    [Tooltip("3등 스킬 인첸트 데미지")]
    [SerializeField] private TMP_Text _enchantDamage3;
    [Tooltip("3등 스킬 인첸트 이미지")]
    [SerializeField] private Image _enchantImage3;

    // ---------- Compensation : 보상(코인/양피지) ----------
    [Header("Compensation")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _parchmentText;
    [SerializeField] private TMP_Text _diamondText;

    [Header("보상 슬롯")]
    [SerializeField] private RectTransform _compensationBoard;
    [SerializeField] private RectTransform _goodsRoot;
    [SerializeField] private ResultRewardSlotView[] _rewardSlots;
    [Tooltip("보상 슬롯이 3개 이하일 때 CompensationBoard 높이 비율입니다.")]
    [SerializeField, Range(0.25f, 1f)] private float _compactBoardHeightRatio = 0.5f;

    // ---------- Button : 다시하기 / 다음 챕터 / 로비 ----------
    [Header("Button")]
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _nextChapterButton;
    [SerializeField] private Button _lobbyButton;

    private readonly Dictionary<int, Sprite> _currencyIcons = new Dictionary<int, Sprite>();
    private readonly List<ResultRewardSlotView> _runtimeRewardSlots = new List<ResultRewardSlotView>();
    private Vector2 _defaultCompensationBoardSize;
    private bool _rewardSlotInitialized;
    
    private void Awake()
    {
        if (_retryButton != null)       _retryButton.onClick.AddListener(Retry);
        if (_nextChapterButton != null) _nextChapterButton.onClick.AddListener(NextChapter);
        if (_lobbyButton != null)       _lobbyButton.onClick.AddListener(GoLobby);

        InitializeRewardSlots();
    }

    private void OnDestroy()
    {
        if (_retryButton != null)       _retryButton.onClick.RemoveListener(Retry);
        if (_nextChapterButton != null) _nextChapterButton.onClick.RemoveListener(NextChapter);
        if (_lobbyButton != null)       _lobbyButton.onClick.RemoveListener(GoLobby);
    }

    /// <summary>결산 결과 한 번에 표시 후 팝업 열기</summary>
    public void Show(bool isClear, int maxCombo, long maxDamage,
                     long enchantDamage1, long enchantDamage2, long enchantDamage3,
                     long coin, long parchment)
    {
        Show(isClear, maxCombo, maxDamage, enchantDamage1, enchantDamage2, enchantDamage3, coin, parchment, 0);
    }

    public void Show(bool isClear, int maxCombo, long maxDamage,
                     long enchantDamage1, long enchantDamage2, long enchantDamage3,
                     long coin, long parchment, long _diamond)
    {
        SetResult(isClear);
        SetRecord(maxCombo, maxDamage);
        SetEnchants(enchantDamage1, enchantDamage2, enchantDamage3);
        SetRewards(coin, parchment, _diamond);
        SetRewardSlots(CreateFallbackRewardEntries(coin, parchment, _diamond));

        // 클리어가 아니면(오버) 다음 챕터 버튼 비활성화
        if (_nextChapterButton != null)
            _nextChapterButton.interactable = isClear;

        Open();
    }

    public void Show(bool isClear, int maxCombo, long maxDamage,
                     ResultEnchantEntry[] topEnchantEntries,
                     long coin, long parchment)
    {
        Show(isClear, maxCombo, maxDamage, topEnchantEntries, coin, parchment, 0);
    }

    public void Show(bool isClear, int maxCombo, long maxDamage,
                     ResultEnchantEntry[] topEnchantEntries,
                     long coin, long parchment, long _diamond)
    {
        SetResult(isClear);
        SetRecord(maxCombo, maxDamage);
        SetEnchants(topEnchantEntries);
        SetRewards(coin, parchment, _diamond);
        SetRewardSlots(CreateFallbackRewardEntries(coin, parchment, _diamond));

        if (_nextChapterButton != null)
            _nextChapterButton.interactable = isClear;

        Open();
    }

    public void Show(bool isClear, int maxCombo, long maxDamage,
                     ResultEnchantEntry[] topEnchantEntries,
                     long coin, long parchment, long _diamond,
                     IReadOnlyList<ResultRewardEntry> _rewardEntries)
    {
        SetResult(isClear);
        SetRecord(maxCombo, maxDamage);
        SetEnchants(topEnchantEntries);
        SetRewards(coin, parchment, _diamond);
        SetRewardSlots(_rewardEntries);

        if (_nextChapterButton != null)
            _nextChapterButton.interactable = isClear;

        Open();
    }

    public void SetResult(bool isClear)
    {
        if (_headerText != null)
            _headerText.text = isClear ? _clearText : _overText;
    }

    public void SetRecord(int maxCombo, long maxDamage)
    {
        if (_maxComboText != null)  _maxComboText.text  = $"{maxCombo}";
        if (_maxDamageText != null) _maxDamageText.text = FormatK(maxDamage);
    }

    public void SetEnchants(long damage1, long damage2, long damage3)
    {
        // ToDo : 차후 폴리싱 작업 때 인첸트 이미지도 같이 바뀌도록 수정
        if (_enchantDamage1 != null) _enchantDamage1.text = FormatK(damage1);  // 예: 4.9K
        if (_enchantDamage2 != null) _enchantDamage2.text = FormatK(damage2);
        if (_enchantDamage3 != null) _enchantDamage3.text = FormatK(damage3);
    }

    public void SetEnchants(ResultEnchantEntry[] entries)
    {
        ApplyEnchantSlot(entries, 0, _enchantDamage1, _enchantImage1);
        ApplyEnchantSlot(entries, 1, _enchantDamage2, _enchantImage2);
        ApplyEnchantSlot(entries, 2, _enchantDamage3, _enchantImage3);
    }

    public void SetRewards(long coin, long parchment)
    {
        SetRewards(coin, parchment, 0);
    }

    public void SetRewards(long coin, long parchment, long _diamond)
    {
        if (_coinText != null)      _coinText.text      = FormatK(coin);
        if (_parchmentText != null) _parchmentText.text = FormatK(parchment);
        if (_diamondText != null)   _diamondText.text   = FormatK(_diamond);
    }

    public void SetRewardSlots(IReadOnlyList<ResultRewardEntry> _rewardEntries)
    {
        InitializeRewardSlots();

        int _entryCount = _rewardEntries != null ? Mathf.Min(_rewardEntries.Count, MaxRewardSlotCount) : 0;
        EnsureRewardSlotCount(_entryCount);

        for (int _index = 0; _index < _runtimeRewardSlots.Count; _index++)
        {
            if (_index >= _entryCount)
            {
                _runtimeRewardSlots[_index].Hide();
                continue;
            }

            ResultRewardEntry _entry = _rewardEntries[_index];
            Sprite _iconSprite = ResolveCurrencyIcon(_entry._itemId);
            if (_iconSprite == null)
            {
                _iconSprite = _runtimeRewardSlots[_index].CurrentIcon;
            }

            _runtimeRewardSlots[_index].SetReward(_entry, _iconSprite, FormatK(_entry._amount));
        }

        ApplyRewardBoardLayout(_entryCount);
    }

    public void Open()
    {
        if (_navigator != null) _navigator.ShowSettlement();
    }

    public void Close()
    {
        if (_navigator != null) _navigator.HideSettlement();
    }
    
    private void Retry()
    {
        Close();
        OnRetryClicked?.Invoke();
    }

    private void NextChapter()
    {
        Close();
        OnNextChapterClicked?.Invoke();
    }

    private void GoLobby()
    {
        if (_navigator != null) _navigator.ToLobbyAction();
    }
    
    private static string FormatK(long value)
    {
        if (value < 1000) return value.ToString();
        if (value < 1_000_000) return (value / 1000f).ToString("0.#") + "K";
        return (value / 1_000_000f).ToString("0.#") + "M";
    }

    private void InitializeRewardSlots()
    {
        if (_rewardSlotInitialized)
        {
            return;
        }

        _rewardSlotInitialized = true;
        ResolveRewardRoots();

        if (_compensationBoard != null)
        {
            _defaultCompensationBoardSize = _compensationBoard.sizeDelta;
        }

        _runtimeRewardSlots.Clear();

        if (_rewardSlots != null)
        {
            for (int _index = 0; _index < _rewardSlots.Length; _index++)
            {
                AddRewardSlot(_rewardSlots[_index]);
            }
        }

        if (_goodsRoot != null)
        {
            for (int _index = 0; _index < _goodsRoot.childCount; _index++)
            {
                ResultRewardSlotView _slot = _goodsRoot.GetChild(_index).GetComponent<ResultRewardSlotView>();
                if (_slot == null)
                {
                    _slot = _goodsRoot.GetChild(_index).gameObject.AddComponent<ResultRewardSlotView>();
                }

                AddRewardSlot(_slot);
            }
        }

        CacheCurrencyIcon(GoldId, _coinText);
        CacheCurrencyIcon(ParchmentId, _parchmentText);
        CacheCurrencyIcon(DiamondId, _diamondText);
    }

    private void ResolveRewardRoots()
    {
        if (_goodsRoot == null)
        {
            Transform _goodsTransform = FindChildByName(transform, "Goods");
            _goodsRoot = _goodsTransform as RectTransform;
        }

        if (_compensationBoard == null)
        {
            Transform _boardTransform = FindChildByName(transform, "CompensationBoard");
            _compensationBoard = _boardTransform as RectTransform;
        }
    }

    private void AddRewardSlot(ResultRewardSlotView _slot)
    {
        if (_slot == null || _runtimeRewardSlots.Contains(_slot))
        {
            return;
        }

        _slot.InitializeIfNeeded();
        _runtimeRewardSlots.Add(_slot);
    }

    private void EnsureRewardSlotCount(int _entryCount)
    {
        if (_entryCount <= _runtimeRewardSlots.Count || _goodsRoot == null || _runtimeRewardSlots.Count == 0)
        {
            return;
        }

        int _targetCount = Mathf.Min(_entryCount, MaxRewardSlotCount);
        while (_runtimeRewardSlots.Count < _targetCount)
        {
            int _sourceLimit = Mathf.Min(CompactRewardSlotThreshold, _runtimeRewardSlots.Count);
            int _sourceIndex = _runtimeRewardSlots.Count % _sourceLimit;
            ResultRewardSlotView _sourceSlot = _runtimeRewardSlots[_sourceIndex];
            ResultRewardSlotView _slot = Instantiate(_sourceSlot, _goodsRoot);
            _slot.name = $"RewardSlot_{_runtimeRewardSlots.Count + 1}";
            _slot.InitializeIfNeeded();
            _runtimeRewardSlots.Add(_slot);
        }
    }

    private void ApplyRewardBoardLayout(int _entryCount)
    {
        Vector2 _boardSize = _defaultCompensationBoardSize;
        if (_compensationBoard != null && _defaultCompensationBoardSize != Vector2.zero)
        {
            Vector2 _size = _defaultCompensationBoardSize;
            if (_entryCount <= CompactRewardSlotThreshold)
            {
                _size.y = _defaultCompensationBoardSize.y * _compactBoardHeightRatio;
            }

            _compensationBoard.sizeDelta = _size;
            _boardSize = _size;
        }

        if (_goodsRoot == null)
        {
            return;
        }

        HorizontalLayoutGroup _layoutGroup = _goodsRoot.GetComponent<HorizontalLayoutGroup>();
        if (_layoutGroup != null)
        {
            _layoutGroup.enabled = false;
        }

        CenterGoodsRoot(_boardSize);
        PositionRewardSlots(_entryCount, _boardSize);
    }

    private void CenterGoodsRoot(Vector2 _boardSize)
    {
        _goodsRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _goodsRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _goodsRoot.pivot = new Vector2(0.5f, 0.5f);
        _goodsRoot.anchoredPosition = Vector2.zero;
        _goodsRoot.sizeDelta = _boardSize != Vector2.zero ? _boardSize : _goodsRoot.sizeDelta;
    }

    private void PositionRewardSlots(int _entryCount, Vector2 _boardSize)
    {
        if (_entryCount <= 0)
        {
            return;
        }

        float _boardWidth = _boardSize.x > 0f ? _boardSize.x : 1070f;
        float _boardHeight = _boardSize.y > 0f ? _boardSize.y : 420f;
        float _columnSpacing = Mathf.Min(330f, _boardWidth / 3.25f);
        float _rowSpacing = _entryCount <= CompactRewardSlotThreshold ? 0f : Mathf.Min(400f, _boardHeight * 0.42f);
        float _topY = _entryCount <= CompactRewardSlotThreshold ? 60f : _rowSpacing * 0.5f + 40f;

        for (int _index = 0; _index < _runtimeRewardSlots.Count; _index++)
        {
            RectTransform _slotRect = _runtimeRewardSlots[_index].transform as RectTransform;
            if (_slotRect == null)
            {
                continue;
            }

            int _column = _index % CompactRewardSlotThreshold;
            int _row = _index / CompactRewardSlotThreshold;

            _slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            _slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            _slotRect.pivot = new Vector2(0.5f, 0.5f);
            _slotRect.anchoredPosition = new Vector2((_column - 1) * _columnSpacing, _topY - _row * _rowSpacing);
        }
    }

    private ResultRewardEntry[] CreateFallbackRewardEntries(long _coin, long _parchment, long _diamond)
    {
        List<ResultRewardEntry> _entries = new List<ResultRewardEntry>(CompactRewardSlotThreshold);
        AddRewardEntry(_entries, GoldId, _coin, "반복 보상");
        AddRewardEntry(_entries, ParchmentId, _parchment, "반복 보상");
        AddRewardEntry(_entries, DiamondId, _diamond, "반복 보상");
        return _entries.ToArray();
    }

    private static void AddRewardEntry(List<ResultRewardEntry> _entries, int _itemId, long _amount, string _label)
    {
        if (_amount <= 0)
        {
            return;
        }

        _entries.Add(new ResultRewardEntry(_itemId, _amount, _label));
    }

    private void CacheCurrencyIcon(int _itemId, TMP_Text _text)
    {
        if (_currencyIcons.ContainsKey(_itemId))
        {
            return;
        }

        ResultRewardSlotView _slot = FindSlotByText(_text);
        if (_slot == null || _slot.CurrentIcon == null)
        {
            return;
        }

        _currencyIcons.Add(_itemId, _slot.CurrentIcon);
    }

    private Sprite ResolveCurrencyIcon(int _itemId)
    {
        return _currencyIcons.TryGetValue(_itemId, out Sprite _sprite) ? _sprite : null;
    }

    private ResultRewardSlotView FindSlotByText(TMP_Text _text)
    {
        if (_text == null || _goodsRoot == null)
        {
            return null;
        }

        Transform _current = _text.transform;
        while (_current != null && _current.parent != null)
        {
            if (_current.parent == _goodsRoot)
            {
                return _current.GetComponent<ResultRewardSlotView>();
            }

            _current = _current.parent;
        }

        return null;
    }

    private static Transform FindChildByName(Transform _root, string _name)
    {
        if (_root == null)
        {
            return null;
        }

        for (int _index = 0; _index < _root.childCount; _index++)
        {
            Transform _child = _root.GetChild(_index);
            if (_child.name == _name)
            {
                return _child;
            }

            Transform _found = FindChildByName(_child, _name);
            if (_found != null)
            {
                return _found;
            }
        }

        return null;
    }

    private void ApplyEnchantSlot(ResultEnchantEntry[] entries, int index, TMP_Text damageText, Image iconImage)
    {
        ResultEnchantEntry _entry = GetEnchantEntry(entries, index);

        if (damageText != null)
        {
            damageText.text = FormatK(_entry._damage);
        }

        if (iconImage == null)
        {
            return;
        }

        string _imageKey = ResolveEnchantImageKey(_entry._skillId);
        EnchantIconLoader.ApplyIcon(iconImage, _imageKey);
    }

    private static ResultEnchantEntry GetEnchantEntry(ResultEnchantEntry[] entries, int index)
    {
        if (entries == null || index < 0 || index >= entries.Length)
        {
            return new ResultEnchantEntry(0, 0);
        }

        return entries[index];
    }

    private string ResolveEnchantImageKey(int skillId)
    {
        if (skillId <= 0)
        {
            return string.Empty;
        }

        if (TryGetOwnedSkillImageKey(skillId, out string _ownedImageKey))
        {
            return _ownedImageKey;
        }

        var _repo = DataManager.Instance != null ? DataManager.Instance.SpellRepo : null;
        if (_repo == null)
        {
            return string.Empty;
        }

        int _levelOneSkillId = ConvertStandardIdToSkillTableId(skillId, 1);
        var _skillData = _repo.GetSkillData(_levelOneSkillId);
        if (_skillData == null || _skillData.SkillIcon_ID <= 0)
        {
            return string.Empty;
        }

        return $"{_skillData.SkillIcon_ID}";
    }

    private static bool TryGetOwnedSkillImageKey(int skillId, out string imageKey)
    {
        imageKey = string.Empty;

        var _enchantModel = FindFirstObjectByType<EnchantModel>();
        if (_enchantModel == null)
        {
            return false;
        }

        foreach (var _pair in _enchantModel.OwnedSkills)
        {
            var _skillData = _pair.Value.Data;
            if (_skillData == null || _skillData.SkillIcon_ID <= 0)
            {
                continue;
            }

            if (ConvertSkillTableIdToStandardId(_skillData.Skill_ID) != skillId)
            {
                continue;
            }

            imageKey = $"{_skillData.SkillIcon_ID}";
            return true;
        }

        return false;
    }

    private static int ConvertSkillTableIdToStandardId(int skillTableId)
    {
        int _baseId = skillTableId / 10;
        int _element = _baseId / 1000;
        int _index = _baseId % 1000;

        if (_index > 9)
        {
            _index %= 10;
        }

        return _element * 100 + _index;
    }

    private static int ConvertStandardIdToSkillTableId(int standardId, int level)
    {
        int _legacySkillId = standardId * 10 + Mathf.Clamp(level, 1, 3);
        return MapLegacySkillIdToSkillTableId(_legacySkillId);
    }

    private static int MapLegacySkillIdToSkillTableId(int legacySkillId)
    {
        switch (legacySkillId)
        {
            case 2011:
                return 20111;
            case 2012:
                return 20112;
            case 2013:
                return 20113;
            default:
                return (legacySkillId / 1000) * 10000 + (legacySkillId % 1000);
        }
    }

    public void DisableButtonForScenarioPlay(bool disable)
    {
        if(_retryButton != null) _retryButton.interactable = !disable;
        if(_nextChapterButton != null) _nextChapterButton.interactable = !disable;
    }
}


