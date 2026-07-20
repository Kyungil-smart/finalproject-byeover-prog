// 담당자 : 정승우
// 설명   : 현지화 텍스트 관리 -- 한/영 기반, 확장 가능 구조

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 언어에 맞는 텍스트를 반환한다.
/// 언어 전환 시 OnLanguageChanged를 발행해서 모든 View가 갱신된다.
/// 지금은 한/영 2개지만, 나중에 언어 추가할 때 LanguageEntry에 필드만 추가하면 됨.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    // ---------- 이벤트 ----------
    public event Action OnLanguageChanged;

    // ---------- SerializeField ----------
    [Header("레거시 데이터 -> 미 사용 시 팀장님 삭제 요망")]
    [SerializeField] private Legacy_LanguageTable _languageTable;
    
    [Header("번역 테이블")]
    [SerializeField] private EnchantLocalizationTable _enchantTable;
    [SerializeField] private GearLocalizationTable _gearTable;
    [SerializeField] private UILocalizationTable _uiTable;
    [SerializeField] private HousingLocalizationTable _housingTable;
    [SerializeField] private ChapterLocalizationTable _chapterTable;

    // ---------- Dictionary ----------
    private Dictionary<string, Legacy_LanguageEntry> _entries;
    private Dictionary<int, LocalizationData> _enchantLocalizingData;
    private Dictionary<int, LocalizationData> _gearLocalizingData;
    private Dictionary<int, LocalizationData> _uiLocalizingData;
    private Dictionary<int, LocalizationData> _housingLocalizingData;
    private Dictionary<int, LocalizationData> _chapterLocalizingData;
    private Dictionary<LocalizingType, Dictionary<int, LocalizationData>> _localizingDictionary;
    
    // ---------- 상태 ----------
    private string _currentLang;  // "ko" or "en"

    public string CurrentLanguage => _currentLang;
    
    private bool _isInitialized;

    // ---------- 초기화 ----------
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void Initialize()
    {
        _entries = BuildDictionary(_languageTable, nameof(_languageTable), r => r.Key);
        _enchantLocalizingData = BuildDictionary(_enchantTable, nameof(_enchantTable), r => r.Language_ID);
        _gearLocalizingData = BuildDictionary(_gearTable, nameof(_gearTable), r => r.Language_ID);
        _uiLocalizingData = BuildDictionary(_uiTable, nameof(_uiTable), r => r.Language_ID);
        _housingLocalizingData = BuildDictionary(_housingTable, nameof(_housingTable), r => r.Language_ID);
        _chapterLocalizingData = BuildDictionary(_chapterTable, nameof(_chapterTable), r => r.Language_ID);
        _localizingDictionary = BuildLocalizationDictionary();

        // 저장된 언어 설정 불러오기
        string saved = PlayerPrefs.GetString("Language", "");

        if (!string.IsNullOrEmpty(saved))
        {
            _currentLang = saved;
        }
        else
        {
            // 기기 언어 자동 감지
            _currentLang = Application.systemLanguage == SystemLanguage.Korean ? "ko" : "en";
        }

        _isInitialized = true;
        Debug.Log($"[Localization] 초기화 완료. {_entries.Count}개 키 로드. 현재 언어: {_currentLang}");
    }

    // ---------- 텍스트 조회 ----------
    public string Get(string key)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[Localization] Not initialized. Return Key Code");
            return $"[{key}]";
        }
        
        if (!_entries.TryGetValue(key, out var entry))
        {
            Debug.LogWarning($"[Localization] {key} is not found. Return Key Code");
            return $"[{key}]";
        }

        // 현재 언어에 맞는 텍스트 반환
        // 나중에 언어 추가할 때 여기에 case 추가하면 됨
        switch (_currentLang)
        {
            case "ko": return entry.Ko;
            case "en": return entry.En;
            default:   return entry.En;
        }
    }

    // 포맷 파라미터 있는 버전
    // 시트: "ENCHANT_DESC_3001" -> "매직 미사일 데미지가 {0}% 증가"
    // 사용: Get("ENCHANT_DESC_3001", 15) -> "매직 미사일 데미지가 15% 증가"
    public string Get(string key, params object[] args)
    {
        string template = Get(key);
        if (template.StartsWith("[")) return template;

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            // 포맷 파라미터가 안 맞으면 원본 반환
            Debug.LogWarning($"[Localization] 포맷 에러: key={key}, args={args.Length}개");
            return template;
        }
    }
    
    public string Get(int id, LocalizingType localizingType)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[Localization] 초기화 안됨. Return Localizing Code");
            return $"[{id}]";
        }
        
        if (!_localizingDictionary.TryGetValue(localizingType, out var targetDictionary))
        {
            Debug.LogWarning($"[Localization] {localizingType} Dictionary is Not Found. Return Localizing Code");
            return $"[{id}]";
        }
        
        if (!targetDictionary.TryGetValue(id, out var entry))
        {
            Debug.LogWarning($"[Localization] ID{id} data is not found in {localizingType} Dictionary. Return Localizing Code");
            return $"[{id}]";
        }
        
        // 현재 언어에 맞는 텍스트 반환
        // 나중에 언어 추가할 때 여기에 case 추가하면 됨
        // 성공 경로 로그 금지: UI 목록 하나 열 때마다 조회 수십 회가 발생해(호출부 50여 곳)
        // 릴리스에서도 문자열 할당 + 로그 비용이 그대로 쌓인다.
        switch (_currentLang)
        {
            case "ko": return entry.KR;
            case "en": return entry.EN;
            default:   return entry.EN;
        }
    }

    // 포맷 파라미터 있는 버전
    // 시트: "ENCHANT_DESC_3001" -> "매직 미사일 데미지가 {0}% 증가"
    // 사용: Get("ENCHANT_DESC_3001", 15) -> "매직 미사일 데미지가 15% 증가"
    public string Get(int id, LocalizingType localizingType, params object[] args)
    {
        string template = Get(id, localizingType);
        if (template.StartsWith("[") || args == null || args.Length == 0) 
            return template;

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            // 포맷 파라미터가 안 맞으면 원본 반환
            Debug.LogWarning($"[Localization] 포맷 에러: key={id}, args={args.Length}개");
            return template;
        }
    }

    // ---------- 언어 전환 ----------
    public void SetLanguage(string langCode)
    {
        _currentLang = langCode;
        PlayerPrefs.SetString("Language", langCode);
        PlayerPrefs.Save();

        // 이걸 쏘면 모든 View의 ApplyTexts()가 호출됨
        OnLanguageChanged?.Invoke();

        Debug.Log($"[Localization] 언어 변경: {langCode}");
    }

    // 간편 토글 (옵션 화면에서)
    public void ToggleLanguage()
    {
        if (_currentLang == "ko")
            SetLanguage("en");
        else
            SetLanguage("ko");
    }
    
    // ---------- 보조 함수 ----------
    private Dictionary<TKey, TData> BuildDictionary<TData, TKey>(
        DataTable<TData> table,
        string tableName,
        Func<TData, TKey> keySelector)
        where TData : class
    {
        var result = new Dictionary<TKey, TData>();

        if (table == null)
        {
            Debug.LogWarning($"[Localization] {tableName} is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[Localization] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[Localization] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[Localization] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }

    private Dictionary<LocalizingType, Dictionary<int, LocalizationData>> BuildLocalizationDictionary()
    {
        var result = new Dictionary<LocalizingType, Dictionary<int, LocalizationData>>();
        
        result.Add(LocalizingType.Enchant, _enchantLocalizingData);
        result.Add(LocalizingType.Gear, _gearLocalizingData);
        result.Add(LocalizingType.UI, _uiLocalizingData);
        result.Add(LocalizingType.Housing, _housingLocalizingData);
        result.Add(LocalizingType.Chapter, _chapterLocalizingData);
        
        return  result;
    }
    
}
