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
    // ---------- 싱글톤 ----------
    // _Boot 씬에만 1개 존재. 씬 전환(_Boot→_Lobby→_InGame)에도 살아남도록 DontDestroyOnLoad.
    // _Lobby/_InGame의 View들은 씬 간 직렬화 참조가 불가능하므로 이 Instance로 자가 탐색한다.
    public static LocalizationManager Instance { get; private set; }

    // ---------- 이벤트 ----------
    public event Action OnLanguageChanged;

    // ---------- SerializeField ----------
    [Header("데이터")]
    [Tooltip("언어 테이블 SO")]
    [SerializeField] private Legacy_LanguageTable _languageTable;

    // ---------- 상태 ----------
    private Dictionary<string, Legacy_LanguageEntry> _entries;
    private string _currentLang;  // "ko" or "en"

    public string CurrentLanguage => _currentLang;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------- 초기화 ----------
    public void Initialize()
    {
        _entries = new Dictionary<string, Legacy_LanguageEntry>();

        if (_languageTable == null)
        {
            Debug.LogWarning("[Localization] LanguageTable SO가 연결 안 됨. Inspector 확인.");
            return;
        }

        for (int i = 0; i < _languageTable.rows.Count; i++)
        {
            var entry = _languageTable.rows[i];
            if (string.IsNullOrEmpty(entry.Key)) continue;

            if (_entries.ContainsKey(entry.Key))
            {
                Debug.LogWarning($"[Localization] 중복 키: {entry.Key}");
                continue;
            }

            _entries[entry.Key] = entry;
        }

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

        Debug.Log($"[Localization] 초기화 완료. {_entries.Count}개 키 로드. 현재 언어: {_currentLang}");
    }

    // ---------- 텍스트 조회 ----------
    public string Get(string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            // 키 없으면 눈에 띄게 표시. 게임 화면에서 바로 보임.
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
}
