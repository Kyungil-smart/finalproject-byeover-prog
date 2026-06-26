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

    // ---------- Dictionary ----------
    private Dictionary<string, Legacy_LanguageEntry> _entries;
    private Dictionary<int, LocalizationData> _enchantLocalizingData;
    
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
            Debug.LogWarning("[Localization] Not initialized. Return Localizing Code");
            return $"[{id}]";
        }
        
        LocalizationData entry = null;
        
        switch (localizingType)
        {
            case  LocalizingType.Enchant:
                if (!_enchantLocalizingData.TryGetValue(id, out entry))
                {
                    Debug.LogWarning($"[Localization] {id} is not found. Return Localizing Code");
                    return $"[{id}]";
                }
                break;
        }
        
        if(entry == null)
        {
            Debug.LogWarning($"[Localization] {localizingType} is wrong Localizing type of this id : {id}. Return Localizing Code");
            return $"[{id}]";
        }
        
        // 현재 언어에 맞는 텍스트 반환
        // 나중에 언어 추가할 때 여기에 case 추가하면 됨
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
        if (template.StartsWith("[")) return template;

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
}
