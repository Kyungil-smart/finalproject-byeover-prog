// 작성자 : 홍정옥
// 설명 : 게임 오버/클리어 시 뜨는 결산(Result) 팝업 - 결과/기록/인챈트/보상/버튼 표시

// 수정자 : 김영찬
// 설명 : 팝업 개/폐시 ScreenNavigator의 함수를 사용하도록 수정 및 로비로 돌아가는 키 입력에 대한 기능 ScreenNavigator와 통합

// 수정자 : 김영찬
// 설명 : Ingamebootstrap.cs 와의 연결 재 구성

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPopup : MonoBehaviour
{
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

    // ---------- Button : 다시하기 / 다음 챕터 / 로비 ----------
    [Header("Button")]
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _nextChapterButton;
    [SerializeField] private Button _lobbyButton;
    
    private void Awake()
    {
        if (_retryButton != null)       _retryButton.onClick.AddListener(Retry);
        if (_nextChapterButton != null) _nextChapterButton.onClick.AddListener(NextChapter);
        if (_lobbyButton != null)       _lobbyButton.onClick.AddListener(GoLobby);
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
        SetResult(isClear);
        SetRecord(maxCombo, maxDamage);
        SetEnchants(enchantDamage1, enchantDamage2, enchantDamage3);
        SetRewards(coin, parchment);

        // 클리어가 아니면(오버) 다음 챕터 버튼 비활성화
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

    public void SetRewards(long coin, long parchment)
    {
        if (_coinText != null)      _coinText.text      = FormatK(coin);
        if (_parchmentText != null) _parchmentText.text = FormatK(parchment);
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
        if (GameManager.Instance != null)
            GameManager.Instance.LoadInGame();   // 현재 스테이지 다시 시작
    }

    private void NextChapter()
    {
        Close();
        OnNextChapterClicked?.Invoke();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectedChapterId += 1;
            GameManager.Instance.LoadInGame();
        }
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
}


