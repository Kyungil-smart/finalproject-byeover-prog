//담당자: 조규민
//설명: 도서관 하우징 프로토타입의 해금, 구매, 착용 상태를 관리한다.

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 하우징 가구의 해금, 구매, 착용 상태와 임시 저장 흐름을 담당한다.
/// </summary>
public class HousingModel : MonoBehaviour
{
    [Header("진행 기준")]
    [Tooltip("하우징 해금 조건을 판단할 로비 진행도 모델입니다. 비어 있으면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private PlayerProgressModel _progressModel;

    [Header("가구 정의")]
    [Tooltip("비어 있으면 도서관 하우징 기본 가구 정의를 자동으로 사용합니다.")]
    [SerializeField] private List<HousingFurnitureDefinition> _furnitureDefinitions = new List<HousingFurnitureDefinition>();

    public event Action StateChanged;
    public event Action<int, int> SlotEquippedChanged;
    public event Action<int> FurniturePurchased;

    private const string OWNED_KEY = "Housing_OwnedFurnitureIds";
    private const string EQUIPPED_SLOT_KEY_PREFIX = "Housing_EquippedSlot_";

    private readonly HousingFurnitureRuntimeState _runtimeState = new HousingFurnitureRuntimeState();
    private readonly Dictionary<int, HousingFurnitureDefinition> _definitionsById = new Dictionary<int, HousingFurnitureDefinition>();
    private bool _initialized;

    public int CurrentChapter => _progressModel != null ? _progressModel.CurrentChapter : 1;

    private void Awake()
    {
        // 기능: 시작 시 진행도 모델을 찾고 하우징 상태를 초기화한다.
        ResolveProgressModel();
        Initialize();
    }

    private void OnEnable()
    {
        // 기능: 활성화 시 진행도 변경 이벤트를 모델 상태 갱신에 연결한다.
        ResolveProgressModel();
        BindProgressModel();
    }

    private void OnDisable()
    {
        // 기능: 비활성화 시 진행도 변경 이벤트 연결을 해제한다.
        UnbindProgressModel();
    }

    public void Initialize()
    {
        // 기능: 기본 가구 정의, 저장된 보유/착용 상태, 기본 착용값을 한 번만 구성한다.
        if (_initialized)
            return;

        EnsureDefaultDefinitions();
        BuildDefinitionMap();
        LoadState();
        EnsureDefaultOwnedAndEquipped();
        SaveState();
        _initialized = true;
    }

    public IReadOnlyList<HousingFurnitureDefinition> GetDefinitions()
    {
        // 기능: 전체 가구 정의 목록을 읽기 전용으로 제공한다.
        Initialize();
        return _furnitureDefinitions;
    }

    public List<HousingFurnitureDefinition> GetDefinitionsBySlot(int _slotId)
    {
        // 기능: 특정 슬롯에 착용 가능한 가구 정의만 필터링한다.
        Initialize();

        List<HousingFurnitureDefinition> _definitions = new List<HousingFurnitureDefinition>();
        foreach (HousingFurnitureDefinition _definition in _furnitureDefinitions)
        {
            if (_definition == null || _definition.SlotId != _slotId)
                continue;

            _definitions.Add(_definition);
        }

        return _definitions;
    }

    public bool TryGetDefinition(int _furnitureId, out HousingFurnitureDefinition _definition)
    {
        // 기능: 가구 ID로 고정 가구 정의를 조회한다.
        Initialize();
        return _definitionsById.TryGetValue(_furnitureId, out _definition);
    }

    public bool TryGetEquippedDefinition(int _slotId, out HousingFurnitureDefinition _definition)
    {
        // 기능: 슬롯에 현재 착용된 가구 ID를 실제 정의 데이터로 변환한다.
        Initialize();
        _definition = null;

        if (!_runtimeState.TryGetEquipped(_slotId, out int _furnitureId))
            return false;

        return _definitionsById.TryGetValue(_furnitureId, out _definition);
    }

    public bool IsUnlocked(HousingFurnitureDefinition _definition)
    {
        // 기능: 가구 정의의 해금 챕터를 현재 플레이 진행도와 비교한다.
        return _definition != null && HousingUnlockUtility.IsChapterCleared(_progressModel, _definition.UnlockChapter);
    }

    public bool IsOwned(int _furnitureId)
    {
        // 기능: 런타임 상태에 해당 가구 보유 기록이 있는지 확인한다.
        Initialize();
        return _runtimeState.IsOwned(_furnitureId);
    }

    public bool IsEquipped(int _slotId, int _furnitureId)
    {
        // 기능: 특정 슬롯에 해당 가구가 착용 중인지 확인한다.
        Initialize();
        return _runtimeState.TryGetEquipped(_slotId, out int _equippedId) && _equippedId == _furnitureId;
    }

    public HousingPurchaseResult TryPurchase(int _furnitureId, int _gold, int _parchment)
    {
        // 기능: 해금 여부, 보유 여부, 재화 조건을 검증한 뒤 구매 상태를 저장한다.
        Initialize();

        if (!_definitionsById.TryGetValue(_furnitureId, out HousingFurnitureDefinition _definition))
            return HousingPurchaseResult.InvalidFurniture;

        if (!IsUnlocked(_definition))
            return HousingPurchaseResult.Locked;

        if (_runtimeState.IsOwned(_furnitureId))
            return HousingPurchaseResult.AlreadyOwned;

        if (_gold < _definition.GoldPrice || _parchment < _definition.ParchmentPrice)
            return HousingPurchaseResult.NotEnoughCurrency;

        _runtimeState.AddOwned(_furnitureId);
        SaveState();
        FurniturePurchased?.Invoke(_furnitureId);
        StateChanged?.Invoke();
        return HousingPurchaseResult.Success;
    }

    public HousingPurchaseResult TryEquip(int _slotId, int _furnitureId)
    {
        // 기능: 슬롯 적합성, 해금 여부, 보유 여부를 검증한 뒤 착용 가구를 교체한다.
        Initialize();

        if (!_definitionsById.TryGetValue(_furnitureId, out HousingFurnitureDefinition _definition))
            return HousingPurchaseResult.InvalidFurniture;

        if (_definition.SlotId != _slotId)
            return HousingPurchaseResult.SlotMismatch;

        if (!IsUnlocked(_definition))
            return HousingPurchaseResult.Locked;

        if (!_runtimeState.IsOwned(_furnitureId))
            return HousingPurchaseResult.NotOwned;

        if (IsEquipped(_slotId, _furnitureId))
            return HousingPurchaseResult.AlreadyEquipped;

        _runtimeState.SetEquipped(_slotId, _furnitureId);
        SaveState();
        SlotEquippedChanged?.Invoke(_slotId, _furnitureId);
        StateChanged?.Invoke();
        return HousingPurchaseResult.Success;
    }

    public HousingPurchaseResult TryPurchaseAndEquip(int _slotId, int _furnitureId, int _gold, int _parchment)
    {
        // 기능: 미보유 가구는 구매를 먼저 시도하고, 성공하면 같은 흐름에서 착용까지 처리한다.
        Initialize();

        if (!IsOwned(_furnitureId))
        {
            HousingPurchaseResult _purchaseResult = TryPurchase(_furnitureId, _gold, _parchment);
            if (_purchaseResult != HousingPurchaseResult.Success && _purchaseResult != HousingPurchaseResult.AlreadyOwned)
                return _purchaseResult;
        }

        HousingPurchaseResult _equipResult = TryEquip(_slotId, _furnitureId);
        if (_equipResult == HousingPurchaseResult.AlreadyEquipped)
            return HousingPurchaseResult.Success;

        return _equipResult;
    }

    public string GetStateLabel(HousingFurnitureDefinition _definition)
    {
        // 기능: UI 버튼에 표시할 잠금, 구매, 착용, 착용중 상태 문구를 반환한다.
        Initialize();

        if (_definition == null)
            return "오류";

        if (!IsUnlocked(_definition))
            return "잠금";

        if (!_runtimeState.IsOwned(_definition.FurnitureId))
            return "구매";

        if (IsEquipped(_definition.SlotId, _definition.FurnitureId))
            return "착용중";

        return "착용";
    }

    public string GetUnlockConditionLabel(HousingFurnitureDefinition _definition)
    {
        // 기능: 잠긴 가구의 해금 조건 문구를 UI 표시용으로 만든다.
        Initialize();

        if (_definition == null)
            return string.Empty;

        if (IsUnlocked(_definition))
            return string.Empty;

        return "챕터 " + _definition.UnlockChapter + " 클리어 시 해금";
    }

    private void ResolveProgressModel()
    {
        // 기능: Inspector 미연결 시 씬의 PlayerProgressModel을 찾아 진행도 기준으로 사용한다.
        if (_progressModel != null)
            return;

        _progressModel = FindFirstObjectByType<PlayerProgressModel>(FindObjectsInactive.Include);
    }

    private void BindProgressModel()
    {
        // 기능: 진행도 변경 이벤트를 중복 없이 연결한다.
        if (_progressModel == null)
            return;

        _progressModel.OnProgressUpdated -= HandleProgressUpdated;
        _progressModel.OnProgressUpdated += HandleProgressUpdated;
    }

    private void UnbindProgressModel()
    {
        // 기능: 진행도 변경 이벤트 연결을 해제한다.
        if (_progressModel == null)
            return;

        _progressModel.OnProgressUpdated -= HandleProgressUpdated;
    }

    private void HandleProgressUpdated()
    {
        // 기능: 스테이지 진행도가 바뀌면 새로 해금된 기본 가구를 보유/착용 상태에 반영한다.
        EnsureDefaultOwnedAndEquipped();
        SaveState();
        StateChanged?.Invoke();
    }

    private void EnsureDefaultDefinitions()
    {
        // 기능: 애셋이 없는 프로토타입 단계에서 사용할 도서관 배경, 침대, 책장, 화분 기본 데이터를 만든다.
        if (_furnitureDefinitions.Count > 0)
            return;

        _furnitureDefinitions.Add(new HousingFurnitureDefinition(
            1001,
            HousingSlotId.FullBackground,
            "도서관 배경",
            HousingFurnitureType.Background,
            HousingFurnitureCategory.Background,
            HousingLayerType.Background,
            HousingUiFunctionType.None,
            1,
            0,
            0,
            true,
            "도서관 배경입니다.",
            new Color(0.25f, 0.28f, 0.31f, 1f)));

        _furnitureDefinitions.Add(new HousingFurnitureDefinition(
            1002,
            HousingSlotId.Wallpaper,
            "도서관 벽",
            HousingFurnitureType.Background,
            HousingFurnitureCategory.Background,
            HousingLayerType.Background,
            HousingUiFunctionType.None,
            1,
            0,
            0,
            true,
            "책 냄새가 배어 있는 도서관 벽입니다.",
            new Color(0.62f, 0.53f, 0.43f, 1f)));

        _furnitureDefinitions.Add(new HousingFurnitureDefinition(
            1003,
            HousingSlotId.Floor,
            "도서관 바닥",
            HousingFurnitureType.Background,
            HousingFurnitureCategory.Background,
            HousingLayerType.Background,
            HousingUiFunctionType.None,
            1,
            0,
            0,
            true,
            "차분한 도서관 바닥입니다.",
            new Color(0.42f, 0.32f, 0.23f, 1f)));

        _furnitureDefinitions.Add(new HousingFurnitureDefinition(
            1010,
            HousingSlotId.Bed,
            "도서관 침대",
            HousingFurnitureType.Interaction,
            HousingFurnitureCategory.Large,
            HousingLayerType.LargeFurniture,
            HousingUiFunctionType.None,
            1,
            0,
            0,
            true,
            "침대에 누워 조용히 쉽니다.",
            new Color(0.38f, 0.64f, 0.92f, 1f)));

        _furnitureDefinitions.Add(new HousingFurnitureDefinition(
            1011,
            HousingSlotId.Bookcase,
            "도서관 책장",
            HousingFurnitureType.UiFunction,
            HousingFurnitureCategory.Large,
            HousingLayerType.LargeFurniture,
            HousingUiFunctionType.StoryReplay,
            1,
            0,
            0,
            true,
            "책장에서 지난 이야기를 다시 볼 수 있습니다.",
            new Color(0.34f, 0.28f, 0.23f, 1f)));

        _furnitureDefinitions.Add(new HousingFurnitureDefinition(
            1030,
            HousingSlotId.Plant,
            "도서관 화분",
            HousingFurnitureType.Decoration,
            HousingFurnitureCategory.Small,
            HousingLayerType.SmallFurniture,
            HousingUiFunctionType.None,
            1,
            0,
            0,
            true,
            "도서관 한켠의 조용한 화분입니다.",
            new Color(0.28f, 0.68f, 0.36f, 1f)));
    }

    private void BuildDefinitionMap()
    {
        // 기능: 가구 ID를 키로 빠르게 조회할 수 있도록 정의 맵을 구성한다.
        _definitionsById.Clear();

        foreach (HousingFurnitureDefinition _definition in _furnitureDefinitions)
        {
            if (_definition == null || _definition.FurnitureId <= 0)
                continue;

            if (_definitionsById.ContainsKey(_definition.FurnitureId))
            {
                Debug.LogWarning("[HousingModel] 중복된 가구 ID가 있습니다. " + _definition.FurnitureId, this);
                continue;
            }

            _definitionsById.Add(_definition.FurnitureId, _definition);
        }
    }

    private void EnsureDefaultOwnedAndEquipped()
    {
        // 기능: 기본 보유 가구가 해금되어 있으면 자동 보유 처리하고 빈 슬롯에 기본 착용한다.
        foreach (HousingFurnitureDefinition _definition in _furnitureDefinitions)
        {
            if (_definition == null || !_definition.IsDefaultOwned || !IsUnlocked(_definition))
                continue;

            _runtimeState.AddOwned(_definition.FurnitureId);

            if (_runtimeState.TryGetEquipped(_definition.SlotId, out _))
                continue;

            _runtimeState.SetEquipped(_definition.SlotId, _definition.FurnitureId);
        }
    }

    private void LoadState()
    {
        // 기능: PlayerPrefs에 저장된 보유 가구와 슬롯별 착용 가구를 런타임 상태로 복원한다.
        _runtimeState.Clear();
        LoadOwnedFurniture();
        LoadEquippedFurniture();
    }

    private void LoadOwnedFurniture()
    {
        // 기능: PlayerPrefs 문자열에 저장된 보유 가구 ID 목록을 복원한다.
        string _rawOwnedIds = PlayerPrefs.GetString(OWNED_KEY, string.Empty);
        if (string.IsNullOrWhiteSpace(_rawOwnedIds))
            return;

        string[] _tokens = _rawOwnedIds.Split(',');
        foreach (string _token in _tokens)
        {
            if (!int.TryParse(_token, out int _furnitureId))
                continue;

            _runtimeState.AddOwned(_furnitureId);
        }
    }

    private void LoadEquippedFurniture()
    {
        // 기능: PlayerPrefs에 저장된 슬롯별 착용 가구 ID를 검증 후 복원한다.
        foreach (HousingFurnitureDefinition _definition in _furnitureDefinitions)
        {
            if (_definition == null)
                continue;

            string _key = GetEquippedSlotKey(_definition.SlotId);
            if (!PlayerPrefs.HasKey(_key))
                continue;

            int _furnitureId = PlayerPrefs.GetInt(_key);
            if (!_definitionsById.TryGetValue(_furnitureId, out HousingFurnitureDefinition _equippedDefinition))
                continue;

            if (_equippedDefinition.SlotId != _definition.SlotId)
                continue;

            _runtimeState.SetEquipped(_definition.SlotId, _furnitureId);
        }
    }

    private void SaveState()
    {
        // 기능: 구매/착용 변경 결과를 임시 저장소에 반영해 재진입 시 유지한다.
        SaveOwnedFurniture();
        SaveEquippedFurniture();
        PlayerPrefs.Save();
    }

    private void SaveOwnedFurniture()
    {
        // 기능: 보유 가구 ID 목록을 쉼표 문자열로 직렬화해 저장한다.
        StringBuilder _builder = new StringBuilder();

        foreach (int _furnitureId in _runtimeState.OwnedFurnitureIds)
        {
            if (_builder.Length > 0)
                _builder.Append(',');

            _builder.Append(_furnitureId);
        }

        PlayerPrefs.SetString(OWNED_KEY, _builder.ToString());
    }

    private void SaveEquippedFurniture()
    {
        // 기능: 슬롯별 착용 가구 ID를 개별 PlayerPrefs 키로 저장한다.
        foreach (KeyValuePair<int, int> _pair in _runtimeState.EquippedFurnitureBySlot)
        {
            PlayerPrefs.SetInt(GetEquippedSlotKey(_pair.Key), _pair.Value);
        }
    }

    private string GetEquippedSlotKey(int _slotId)
    {
        // 기능: 슬롯 ID별 착용 저장 키를 생성한다.
        return EQUIPPED_SLOT_KEY_PREFIX + _slotId;
    }
}
