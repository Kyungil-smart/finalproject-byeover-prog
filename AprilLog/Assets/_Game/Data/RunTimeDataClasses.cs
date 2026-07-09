// 생성자 : 김영찬
// 설명 : 인게임에서 사용하는 임시 구조체 및 클래스를 정리하는 스크립트입니다.
// 주의 사항 : 기능 별로 region으로 분류하면 다른 작업자가 보기 편합니다.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#region Sort 보조 구조체

/// <summary>
/// Sort 보조 구조체
/// </summary>
[Serializable]
public struct WaitingCombo
{
    public int[] unitTypes;
    public WaitingDifficulty difficulty;

    public int FilledCount
    {
        get
        {
            int c = 0;
            for (int i = 0; i < unitTypes.Length; i++)
                if (unitTypes[i] >= 0) c++;
            return c;
        }
    }
}

#endregion

#region SpellRepo 지원

/// <summary>
/// 스킬 인첸트 데이터를 이름으로 묶음
/// </summary>
public class SkillNameChainData
{
    public int Name_ID { get; private set; } 
    public int MaxLevel { get; private set; }
    public Dictionary<int, SkillTableData> LevelDataMap { get; private set; }

    public SkillNameChainData(int nameId)
    {
        Name_ID = nameId;
        LevelDataMap = new Dictionary<int, SkillTableData>();
        MaxLevel = 0;
    }

    public void AddData(SkillTableData data)
    {
        LevelDataMap[data.Level] = data;
        if (data.Level > MaxLevel) MaxLevel = data.Level;
    }

    public SkillTableData GetNextLevelData(int currentLevel)
    {
        if (currentLevel >= MaxLevel) return null;
        return LevelDataMap.TryGetValue(currentLevel + 1, out var nextData) ? nextData : null;
    }
}

/// <summary>
/// 스킬 인첸트 데이터를 그룹 ID로 묶음
/// </summary>
public class SkillGroupChainData
{
    public int SkillGroup_ID { get; private set; }
    public Dictionary<int, SkillNameChainData> SkillNameChainData { get; private set; } // Key: Name

    public SkillGroupChainData(int groupId)
    {
        SkillGroup_ID = groupId;
        SkillNameChainData = new Dictionary<int, SkillNameChainData>();
    }

    public void AddData(SkillTableData data)
    {
        if (!SkillNameChainData.ContainsKey(data.Name))
            SkillNameChainData[data.Name] = new SkillNameChainData(data.Name);
        
        SkillNameChainData[data.Name].AddData(data);
    }
}

/// <summary>
/// 스텟 인첸트 데이터를 이름으로 묶음
/// </summary>
public class StatNameChainData
{
    public int Stat_Name_ID { get; private set; }
    public int MaxLevel { get; private set; }
    public Dictionary<int, StatTableData> LevelDataMap { get; private set; }

    public StatNameChainData(int statNameId)
    {
        Stat_Name_ID = statNameId;
        LevelDataMap = new Dictionary<int, StatTableData>();
        MaxLevel = 0;
    }

    public void AddData(StatTableData data)
    {
        LevelDataMap[data.StatLevel] = data;
        if (data.StatLevel > MaxLevel) MaxLevel = data.StatLevel;
    }

    public StatTableData GetNextLevelData(int currentLevel)
    {
        if (currentLevel >= MaxLevel) return null;
        return LevelDataMap.TryGetValue(currentLevel + 1, out var nextData) ? nextData : null;
    }
}

/// <summary>
/// 스텟 인첸트 데이터를 그룹 ID로 묶음
/// </summary>
public class StatGroupChainData
{
    public int StatGroup_ID { get; private set; }
    public Dictionary<int, StatNameChainData> StatNameChainData { get; private set; } // Key: Stat_Name

    public StatGroupChainData(int groupId)
    {
        StatGroup_ID = groupId;
        StatNameChainData = new Dictionary<int, StatNameChainData>();
    }

    public void AddData(StatTableData data)
    {
        if (!StatNameChainData.ContainsKey(data.StatName))
            StatNameChainData[data.StatName] = new StatNameChainData(data.StatName);
        
        StatNameChainData[data.StatName].AddData(data);
    }
}

#endregion

#region GearRepo 지원

/// <summary>
/// 기어 업그레이드 코스트를 저장 및 계산 도우미
/// </summary>
public class GearUpgradeSupporter
{
    public int GearId { get; private set; }
    public int StartLevel { get; private set; }
    public int EndLevel { get; private set; }
    public Dictionary<int, UpgradeCostData> UpgradeCosts { get; private set; }

    public GearUpgradeSupporter(int gearId, int startLevel, int endLevel)
    {
        GearId = gearId;
        StartLevel = startLevel;
        EndLevel = endLevel;
        UpgradeCosts = new Dictionary<int, UpgradeCostData>();
    }

    public void AddData(GearUpgradeCostData data)
    {
        if (!UpgradeCosts.ContainsKey(data.Type))
            UpgradeCosts[data.Type] = new UpgradeCostData(data.Type ,data.BaseAmount, data.GrowthValue);
    }
    
    public int? CalculateUpgradeCosts(int curLevel, int costType)
    {
        if(curLevel < StartLevel || curLevel > EndLevel)
        {
            Debug.LogWarning($"[GearRepo] {curLevel}Level is wrong range Gear Level in this Gear");
            return null;
        }
        
        foreach (var data in UpgradeCosts.Values)
        {
            if (data.Type == costType)
            {
                return Mathf.FloorToInt(data.BaseAmount * data.GrowthValue * curLevel);
            }
        }
        
        return null;
    }
}

public class UpgradeCostData
{
    public int Type { get; private set; }
    public int BaseAmount { get; private set; }
    public float GrowthValue { get; private set; }
    public UpgradeCostData(int type, int baseAmount, float growthValue)
    {
        Type = type;
        BaseAmount = baseAmount;
        GrowthValue = growthValue;
    }
}

#endregion

#region 인첸트 시스템 지원

[Serializable]
public class EnchantCandidate
{
    public EnchantType Type;
    public int Name_ID;            
    public int Specific_ID;        
    public int Level;              
    public float Weight;           
    
    public SkillTableData SkillData; 
    public StatTableData StatData;   
}

[Serializable]
public class EnchantProbabilityConfig
{
    [Header("스킬/스탯 통합 풀 등장 비율 (기본 100 : 100)")]
    public float SkillPoolBaseWeight = 100f;
    public float StatPoolBaseWeight = 100f;

    [Header("스킬 - 1~2개 보유 시 (기획서 3-3-1-1 표의 단계 1)")]
    public float SkillStage1_HeldWeight = 30f;
    public float SkillStage1_UnheldWeight = 70f;

    [Header("스킬 - 3~4개 보유 시 (해당 표의 단계 2)")]
    public float SkillStage2_HeldWeight = 50f;
    public float SkillStage2_UnheldWeight = 50f;

    [Header("스킬 - 5개 보유 시 (해당 표의 단계 3)")]
    public float SkillStage3_HeldWeight = 80f;
    public float SkillStage3_UnheldWeight = 20f;

    [Header("스탯 확률 (기획서 3-3-4)")]
    public float Stat_HeldWeight = 60f;
    public float Stat_UnheldWeight = 40f;
}

[Serializable]
public class EnchantDisplayData
{
    public int EnchantId;
    public string Name;
    public string Description;
    public int Level;
    public string ImageKey;
    public EnchantGroupType TypeLabel;   // 카드 타입 표시용 (Presenter가 stat-type 기반으로 채움)
    public ElementalType ElementalType; // UI에서 이름을 나타낼때
}

[Serializable]
public class AcquiredSkillData
{
    public int Level;
    public int GroupID;
    public SkillTableData Data;
}

[Serializable]
public class AcquiredStatData
{
    public int Level;
    public int GroupID;
    public StatTableData Data;
}

public struct EffectSpec
{
    public int Id { get;  private set; }
    public float CalValue { get; private set; }
    public float CalDuration { get; private set; }
    public float CalInterval { get; private set; }

    public EffectSpec(int id, float calValue, float calDuration, float calInterval)
    {
        Id = id;
        CalValue = calValue;
        CalDuration = calDuration;
        CalInterval = calInterval;
    }
}

public class FusionEnchantData
{
    public int EnchantId { get; private set; }
    public int Sort1 { get; private set; }
    public int Sort2 { get; private set; }
    public int Sort3 { get; private set; }
    public int IconImageKey { get; private set; }

    public FusionEnchantData(int enchantId, int sort1, int sort2, int sort3, int iconImageKey)
    {
        EnchantId = enchantId;
        Sort1 = sort1;
        Sort2 = sort2;
        Sort3 = sort3;
        IconImageKey = iconImageKey;
    }
    
    public void LevelUp()
    {
        EnchantId += 1;
    }
}

[Serializable]
public class EnchantSequenceConfig
{
    [Header("인챈트 등장 순서 (원하는 만큼 추가/수정 가능)")]
    // 기본값: 스킬 -> 스킬 -> 스탯
    public List<EnchantType> DrawSequence = new List<EnchantType> 
    { 
        EnchantType.Skill, 
        EnchantType.Skill, 
        EnchantType.Stat 
    };
}

public static class EnchantGroupIDToEnchantGroupTypeMapper
{
    private static readonly Dictionary<int, EnchantGroupType> _typeMap = new Dictionary<int, EnchantGroupType>
    {
        { EnchantModel.GROUP_NORMAL_SKILL, EnchantGroupType.Normal},
        { EnchantModel.GROUP_COMBINATION_SKILL, EnchantGroupType.Combination},
        { EnchantModel.GROUP_COMBO_SKILL, EnchantGroupType.Combo}
    };

    private static readonly Dictionary<EnchantGroupType, int> _localizingIdMap = new Dictionary<EnchantGroupType, int>
    {
        { EnchantGroupType.Normal, 12011 },
        { EnchantGroupType.Combination, 12012 },
        { EnchantGroupType.Combo, 12013 }
    };
    
    public static EnchantGroupType GetEnchantGroupType(int enchantGroupId)
    {
        return _typeMap.GetValueOrDefault(enchantGroupId, EnchantGroupType.None);
    }

    private static int GetLocalizingId(EnchantGroupType enchantGroupType)
    {
        return _localizingIdMap.GetValueOrDefault(enchantGroupType, -1);
    }

    public static string GetLabelText(EnchantGroupType enchantGroupType)
    {
        if (LocalizationManager.Instance == null)
        {
            if(enchantGroupType == EnchantGroupType.None) return string.Empty;
            
            return enchantGroupType.ToString();
        }
        
        var id = GetLocalizingId(enchantGroupType);
        return id == -1 ? string.Empty : LocalizationManager.Instance.Get(id, LocalizingType.UI);
    }
}

public static class TagToElementalMapper
{
    private static readonly Dictionary<int, ElementalType> _elementalMap = new()
    {
        { 2, ElementalType.Fire },
        { 3, ElementalType.Water },
        { 5, ElementalType.Wind },
        { 7, ElementalType.Lightning },
        { 9, ElementalType.Ice }
    };
        
    public static ElementalType GetElemental(int tagId)
    {
        return _elementalMap.GetValueOrDefault(tagId, ElementalType.None);
    }
}

public static class EnchantColorMapper
{
    private static readonly Dictionary<ElementalType, string> _colorMap = new()
    {
        { ElementalType.Fire, "#C80000" },
        { ElementalType.Ice, "#00C8C8" },
        { ElementalType.Water, "#005AAA" },
        { ElementalType.Wind, "#00AA0E" },
        { ElementalType.Lightning, "#8200AA" }
    };

    private static string GetHexCode(ElementalType type)
    {
        return _colorMap.GetValueOrDefault(type, string.Empty);
    }
    
    public static string SetColorHexCodeText(string text ,ElementalType type)
    {
        var hexCode = GetHexCode(type);
        return string.IsNullOrWhiteSpace(hexCode) ? text : $"<color={hexCode}>{text}</color>";
    }
}


#endregion

#region 인첸트 도감 지원

[Serializable]
public class EnchantBookDisplayData
{
    public int EnchantId;
    public string Name;
    public string Type;
    public bool IsOwned;
    public int Level;
    public int MaxLevel;
}

#endregion

#region PlayerModel 지원

public enum PlayerStatus
{
    Hp,
    Attack,
    CriticalRate,
    CriticalDamage,
    FlatPierce,
    PercentagePierce,
    EffectPower,
    HicCount,
    AoE,
    MaxTargets,
    AttackSpeed
}

public enum CalFormula
{
    Add,
    Rate,
    None
}

#endregion

#region SupplyRepo 지원

[Serializable]
public class ItemContainer
{
    private Dictionary<int, ObscuredInt> _inventory = new (); // Item_ID, 현재 보유량
    private bool _isInitialized;

    public int GetItemCount(int itemId) => GetData(_inventory, itemId);
    public Dictionary<int, ObscuredInt> GetAllItems() => _inventory;

    public void Initialize(Dictionary<int, ItemData> itemInfos)
    {
        foreach (var data in itemInfos.Values)
        {
            SetItemCount(data.Item_ID, 0);
        }
        
        _isInitialized = true;
    }
    
    public void AddItem(int itemId, int amount)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning($"[ResourceRepo] {nameof(ItemContainer)} is not initialized. Skip.");
            return;
        }
        
        if (amount <= 0)
        {
            Debug.LogError($"[ResourceRepo] Wrong Amount. No Add Item ID : {itemId}. Amount : {amount}");
            return;
        }
        
        if (!_inventory.TryAdd(itemId, amount))
        {
            _inventory[itemId] += amount;
        }
        
        Debug.Log($"[ResourceRepo] Item ID : {itemId}. Add Amount : {amount}");
    }
    
    public bool UseItem(int itemId, int amount)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning($"[ResourceRepo] {nameof(ItemContainer)} is not initialized. Skip.");
            return false;
        }
        
        int currentAmount = GetItemCount(itemId);
        
        if (amount == 0)
        {
            Debug.Log($"[ResourceRepo] Item ID : {itemId}. {amount} Use Success. Current Amount : {_inventory[itemId]}");
            return true;
        }
        
        if (amount < 0) 
        {
            Debug.LogError($"[ResourceRepo] Wrong Amount. Return False. Amount: {amount}");
            return false; 
        }
        
        if (currentAmount >= amount)
        {
            _inventory[itemId] -= amount;
            Debug.Log($"[ResourceRepo] Item ID : {itemId}. {amount} Use Success. Current Amount : {_inventory[itemId]}");
            return true;
        }
        
        Debug.Log($"[ResourceRepo] Item ID : {itemId}. {amount} Use Failed. Current Amount : {_inventory[itemId]}");
        return false;
    }
    
    public void SetItemCount(int itemId, int amount)
    {
        _inventory ??= new Dictionary<int, ObscuredInt>();
        _inventory[itemId] = Mathf.Max(0, amount);
    }
    
    private int GetData(Dictionary<int, ObscuredInt> dictionary, int key)
    {
        if (dictionary == null)
        {
            Debug.LogWarning($"[ResourceRepo] Item cache is not initialized. Key: {key}. Return 0");
            return 0;
        }

        if (dictionary.TryGetValue(key, out ObscuredInt data))
            return data;

        Debug.LogWarning($"[ResourceRepo] Item Key is Wrong. Key: {key}. Return 0");
        return 0;
    }
}

[Serializable]
public class StaminaContainer
{
    private Dictionary<int, StaminaSlot> _slots = new ();
    public Dictionary<int, StaminaSlot> Slots => _slots;

    public void Initialize(Dictionary<int, StaminaData> staminaInfos)
    {
        DateTime now = DateTime.Now;
        foreach (var dbData in staminaInfos.Values)
        {
            int savedAmount = dbData.InitialAmount; 
            DateTime savedTime = now; 

            AddOrInitSlot(dbData, savedAmount, savedTime);
            
            GetSlot(dbData.Stamina_ID).CalculateOfflineRecovery(now);
        }
    }
    
    public void AddOrInitSlot(StaminaData dbData, int loadedAmount, DateTime lastSavedTime)
    {
        _slots??= new Dictionary<int, StaminaSlot>();
        
        if (!_slots.ContainsKey(dbData.Stamina_ID))
        {
            _slots.Add(dbData.Stamina_ID, new StaminaSlot
            {
                StaminaID = dbData.Stamina_ID,
                CurrentAmount = loadedAmount,
                MaxOwned = dbData.MaxOwned,
                OverCapMax = dbData.OverCapMax,
                RecoveryTime = dbData.RecoveryTime,
                RecoveryCount = dbData.RecoveryCount,
                RemainTimer = dbData.RecoveryTime,
                LastUpdateTime = lastSavedTime
            });
        }
    }

    public StaminaSlot GetSlot(int id) => GetData(_slots, id);
    
    private TData GetData<TData>(Dictionary<int, TData> dictionary, int key)
        where TData : class
    {
        if (dictionary == null)
        {
            Debug.LogWarning($"[ResourceRepo] Stamina Slot cache is not initialized. Key: {key}");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[ResourceRepo] Stamina Slot data not found. Key: {key}");
        return null;
    }
}

[Serializable]
public class StaminaSlot
{
    public int StaminaID;
    public ObscuredInt CurrentAmount;
    
    // DB 데이터 캐싱
    public int MaxOwned;
    public ObscuredInt OverCapMax;
    public float RecoveryTime;
    public int RecoveryCount;

    // 런타임 데이터 (저장 필요)
    public float RemainTimer;
    public DateTime LastUpdateTime;
    
    // 오프라인 회복 (게임 시작 시 & 백그라운드 복귀 시 호출)
    public void CalculateOfflineRecovery(DateTime now)
    {
        if (CurrentAmount >= MaxOwned)
        {
            RemainTimer = RecoveryTime;
            LastUpdateTime = now;
            return;
        }

        TimeSpan offlineDuration = now - LastUpdateTime;
        float totalSecondsPassed = (float)offlineDuration.TotalSeconds + (RecoveryTime - RemainTimer);

        if (totalSecondsPassed >= RecoveryTime)
        {
            int recoverTicks = (int)(totalSecondsPassed / RecoveryTime);
            int recoverAmount = recoverTicks * RecoveryCount;

            CurrentAmount = Mathf.Min(CurrentAmount + recoverAmount, MaxOwned);
            RemainTimer = RecoveryTime - (totalSecondsPassed % RecoveryTime);
        }
        else
        {
            RemainTimer = RecoveryTime - totalSecondsPassed;
        }

        LastUpdateTime = now;
    }

    // 온라인 실시간 회복 (Update에서 매 프레임 호출)
    public bool TickOnline(float deltaTime)
    {
        if (CurrentAmount >= MaxOwned)
        {
            RemainTimer = RecoveryTime;
            return false;
        }

        RemainTimer -= deltaTime;
        if (RemainTimer <= 0)
        {
            CurrentAmount = Mathf.Min(CurrentAmount + RecoveryCount, MaxOwned);
            RemainTimer += RecoveryTime; 
            LastUpdateTime = DateTime.Now; 
            return true; // 회복 발생
        }
        return false;
    }

    // 포션/다이아 등으로 강제 회복 (OverCapMax까지만)
    public void Add(int amount, out int lossAmount)
    {
        if (amount <= 0)
        {
            Debug.LogError($"[ResourceRepo] Wrong Amount. No Add StaminaID {StaminaID}. Amount : {amount}");
            lossAmount = 0;
            return;
        }
        
        int temp = CurrentAmount + amount;
        
        if (temp > OverCapMax)
        {
            CurrentAmount = OverCapMax;
            lossAmount = temp - OverCapMax;
        }
        
        CurrentAmount = temp;
        lossAmount = 0;
        
        Debug.Log($"[ResourceRepo] Stamina Id {StaminaID} : {amount} Add Success. Loss Amount : {lossAmount}. {CurrentAmount} / {MaxOwned}");
    }
    
    public bool UseStamina(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError($"[ResourceRepo] Wrong Amount. Return False. Amount: {amount}");
            return false;
        }
        
        if (CurrentAmount >= amount)
        {
            CurrentAmount -= amount;
            Debug.Log($"[ResourceRepo] Stamina Id {StaminaID} : {amount} Use Success. {CurrentAmount} / {MaxOwned}");
            return true;
        }
        
        Debug.Log($"[ResourceRepo] Stamina Id {StaminaID} : {amount} Use Failed. {CurrentAmount} / {MaxOwned}");
        return false;
    }
    
    public void SetStamina(int amount, int max)
    {
        if(amount < 0 || max <= 0) return;
        
        CurrentAmount = amount;
        OverCapMax = max;
    }
}

/// <summary>
/// int 타입 변수 암호화 (아이템 및 스태미너 저장소에서 사용, 위변조 방지)
/// </summary>
public struct ObscuredInt
{
    private readonly int _currentKey;
    private readonly int _hiddenValue;

    // int를 ObscuredInt로 만들 때 (암호화)
    private ObscuredInt(int value)
    {
        _currentKey = UnityEngine.Random.Range(1, int.MaxValue); // 랜덤 키 생성
        _hiddenValue = value ^ _currentKey;           // 데이터 숨기기
    }

    // ObscuredInt를 일반 int처럼 읽으려고 할 때 (자동 복호화)
    public static implicit operator int(ObscuredInt obscured)
    {
        return obscured._hiddenValue ^ obscured._currentKey; 
    }

    // 일반 int를 ObscuredInt 변수에 집어넣으려고 할 때 (자동 암호화)
    public static implicit operator ObscuredInt(int value)
    {
        return new ObscuredInt(value);
    }
}

#endregion

#region RewardRepo 지원

[Serializable]
public class RangeData 
{
    public int StartId;
    public int EndId;
    public int DataId;
}

[Serializable]
public class RewardRecipe
{
    public int TargetId;
    public int RewardId;
    public int currentStep;
}

#endregion

#region Lobby 지원

[Serializable]
public class ChapterEntry
{
    [Tooltip("챕터 이름 (예: 어둠의 숲)")]
    public string chapterName;

    [Tooltip("챕터 표시 텍스트 (예: CHAPTER.1) — 비워두면 자동 생성")]
    public string chapterLabel;

    [TextArea(2, 4)]
    [Tooltip("챕터 설명")]
    public string description;
    
    [Tooltip("챕터 대표 이미지")]
    public Sprite image;
}

[CreateAssetMenu(fileName = "ChapterData", menuName = "Lobby/Chapter Data")]
public class ChapterDataSO : ScriptableObject
{
    [Header("챕터 목록 (순서대로)")] 
    public List<ChapterEntry> chapters = new();
    
    private StageRepo _repo;
    private LocalizationManager _localizationManager;
    private bool _isInitialized;

    public int ChapterCount => chapters != null ? chapters.Count : 0;

    public void InitChapters()
    {
        if(_isInitialized) return;
        
        if(chapters.Count > 0)
        {
            _isInitialized = true;
            return;
        }
        
        chapters ??= new List<ChapterEntry>();
        _repo ??= DataManager.Instance.StageRepo;
        _localizationManager ??= LocalizationManager.Instance;
        string path = "Lobby/T";
        
        var indexData = _repo.GetStepIndexToChapterIdMappingData();
        if(indexData == null || indexData.Count == 0)
        {
            Debug.LogWarning("[PageMainLobbyController] 챕터 정보를 불러오지 못했습니다.");
            return;
        }
        
        for (int i = 0; i < indexData.Count; i++)
        {
            var masterData = _repo.GetChapter(indexData[i]);
            string imageIndex = (indexData[i] / 100).ToString();
            
            chapters.Add(
                new ChapterEntry
                {
                    chapterName = _localizationManager != null ? 
                        _localizationManager.Get(masterData.Name, LocalizingType.Chapter) : null,
                    description = _localizationManager != null ?
                        _localizationManager.Get(masterData.Explanation, LocalizingType.Chapter): null,
                    image = Resources.Load<Sprite>(path + imageIndex)
                });
        }
        
        _isInitialized = true;
    }

    public void LanguageChanged()
    {
        if(!_isInitialized || chapters == null || ChapterCount == 0)
        {
            InitChapters();
            return;
        }
        
        var indexData = _repo.GetStepIndexToChapterIdMappingData();
        if(indexData == null || indexData.Count == 0)
        {
            Debug.LogWarning("[PageMainLobbyController] 챕터 정보를 불러오지 못했습니다.");
            return;
        }
        
        for (int i = 0; i < indexData.Count; i++)
        {
            var masterData = _repo.GetChapter(indexData[i]);
            string imageIndex = (indexData[i] / 100).ToString();

            if (chapters[i] == null) continue;
            
            chapters[i].chapterName = _localizationManager != null ? 
                _localizationManager.Get(masterData.Name, LocalizingType.Chapter) : null;
            chapters[i].description = _localizationManager != null ? 
                _localizationManager.Get(masterData.Explanation, LocalizingType.Chapter) : null;
        }
    }
    
    /// <summary>인덱스(0-based)로 챕터 데이터를 반환합니다.</summary>
    public ChapterEntry GetChapter(int index)
    {
        if (chapters == null || chapters.Count == 0) return null;

        index = Mathf.Clamp(index, 0, chapters.Count - 1);
        var entry = chapters[index];

        // label 비어있으면 자동 생성
        if (string.IsNullOrWhiteSpace(entry.chapterLabel))
            entry.chapterLabel = $"CHAPTER.{index + 1}";

        // 이름 비어있으면 자동 생성
        if (string.IsNullOrWhiteSpace(entry.chapterName))
            entry.chapterName = $"Chapter {index + 1}";

        return entry;
    }
}

#endregion