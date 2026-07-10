// 담당자 : 정승우
// 설명   : 화면(팝업) 전환 전담 - SRP: 이것만 Show/Hide 한다

// 수정자 : 김영찬
// 수정내용 : 인게임 UI에 넘겨줄 정보 최신화

// 2차 수정자 : 조규민
// 수정 내용 : 로비 복귀 확인 팝업의 예 버튼에서 GameManager가 없는 직접 실행 환경도 _Lobby 씬으로 복귀하도록 보강

// 3차 수정자 : 김영찬
// 수정 내용 : 씬 전환을 InGameNextSceneLoader.cs에서 일괄 담당하도록 함. 실제 로비 이동 부분을 이벤트 발송만 남김

// 4차 수정자 : 최동훈
// 수정 내용 : 연출 시스템과의 시간 충돌 방지를 위해 IsEffectPlaying 상태값 도입

using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

/// <summary>
/// 인게임 팝업의 Show/Hide를 관리한다.
/// 어떤 Presenter도 다른 View를 직접 조작하지 않고 여기한테 요청한다.
/// </summary>
public class ScreenNavigator : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("팝업 참조")]
    [Tooltip("인챈트 선택 팝업")]
    [SerializeField] private GameObject _enchantSelectPopup;

    [Tooltip("인챈트 교체 팝업")]
    [SerializeField] private GameObject _enchantChangePopup;

    [Tooltip("인첸트 리스트 팝업")]
    [SerializeField] private GameObject _enchantListPopup;

    [Tooltip("정산 화면")]
    [SerializeField] private GameObject _settlementPopup;

    [Tooltip("옵션 팝업")]
    [SerializeField] private GameObject _optionPopup;

    [Tooltip("로비로 이동 팝업")]
    [SerializeField] private GameObject _toLobbyPopup;

    [Tooltip("콤보 텍스트 팝업")]
    [SerializeField] private GameObject _comboTextPopup;

    [Header("특수 웨이브 팝업 참조")]
    [SerializeField] private InGameHUDView _inGameHUDView;
    [SerializeField] private GameObject _rushWavePopup;
    [SerializeField] private GameObject _bossWavePopup;

    [Header("팝업 시간")]
    [SerializeField][Range(0, 10f)] private float _comboPopupTime = 1.5f;
    [SerializeField][Range(0, 10f)] private float _specialWavePopupTime = 3f;

    // ---------- private ----------
    private float _comboPopupTimer;
    private float _wavePopupTimer;

    private bool _comboTimerActive;
    private bool _waveTimerActive;
    private bool _isLoadingLobby;

    // ---------- Event ----------
    public event Action OnLobbyClicked;

    // ---------- 이벤트 구독 & 해제 ----------
    private void OnEnable()
    {
        _inGameHUDView.OnComboTimerActive += ShowComboTextPopup;
        _inGameHUDView.OnSpecialWaveActive += ShowWavePopup;
    }

    private void OnDisable()
    {
        _inGameHUDView.OnComboTimerActive -= ShowComboTextPopup;
        _inGameHUDView.OnSpecialWaveActive -= ShowWavePopup;
    }

    // ---------- 최초에 비활성화 하지만 혹시 모르니 Start에 다시 비활성화 시킴 ----------
    private void Start()
    {
        HideEnchantList();
        HideToLobby();
        HideComboTextPopup();
        HideEnchantChange();
        HideEnchantSelection();
        HideOption();
        HideSettlement();
        HideWavePopup();
        CloseMenu();
    }

    // ---------- 시간 계산 ----------
    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        if (_comboTimerActive)
        {
            _comboPopupTimer += deltaTime;

            if (_comboPopupTimer >= _comboPopupTime)
            {
                HideComboTextPopup();
            }
        }
        if (_waveTimerActive)
        {
            _wavePopupTimer += deltaTime;

            if (_wavePopupTimer >= _specialWavePopupTime)
            {
                HideWavePopup();
            }
        }
    }

    private void ResetComboPopupTimer()
    {
        _comboPopupTimer = 0;
    }

    private void ResetWavePopupTimer()
    {
        _wavePopupTimer = 0;
    }

    // ---------- 인챈트 선택 ----------
    public void ShowEnchantSelection()
    {
        OpenMenu();    // 전투 일시정지 (기획서)
        if (_enchantSelectPopup != null)
            _enchantSelectPopup.SetActive(true);
    }

    public void HideEnchantSelection()
    {
        if (_enchantSelectPopup != null)
            _enchantSelectPopup.SetActive(false);
    }

    // ---------- 인챈트 교체 ----------
    public void ShowEnchantChange()
    {
        OpenMenu();
        if (_enchantChangePopup != null)
            _enchantChangePopup.SetActive(true);
    }

    private void HideEnchantChange()
    {
        if (_enchantChangePopup != null)
            _enchantChangePopup.SetActive(false);
        CloseMenu();
    }

    // ---------- 인챈트 목록 ----------
    public void ShowEnchantList()
    {
        HideOption();
        if (_enchantListPopup != null)
            _enchantListPopup.SetActive(true);
    }

    public void HideEnchantList()
    {
        if (_enchantListPopup != null)
            _enchantListPopup.SetActive(false);
    }

    // ---------- 정산 ----------
    public void ShowSettlement()
    {
        OpenMenu();
        if (_settlementPopup != null)
            _settlementPopup.SetActive(true);
    }

    public void HideSettlement()
    {
        if (_settlementPopup != null)
            _settlementPopup.SetActive(false);
        CloseMenu();
    }

    // ---------- 옵션 ----------
    public void ShowOption()
    {
        HideEnchantList();
        if (_optionPopup != null)
            _optionPopup.SetActive(true);
    }

    private void HideOption()
    {
        if (_optionPopup != null)
            _optionPopup.SetActive(false);
    }

    // ---------- 로비로 이동 팝업 ----------
    public void ShowToLobby()
    {
        if (_toLobbyPopup != null)
            _toLobbyPopup.SetActive(true);
    }

    public void HideToLobby()
    {
        if (_toLobbyPopup != null)
            _toLobbyPopup.SetActive(false);
    }

    // ---------- 게임 일시 정지 / 해제 ----------
    /// <summary>정지형 팝업(인챈트 선택/교체·정산 등)이 열려 있는지. 다른 시스템(데드락 연출 등)이
    /// timeScale을 복구할 때 팝업 정지를 짓밟지 않도록 이 값을 확인해야 한다.</summary>
    public static bool IsMenuOpen { get; private set; }
    public static bool IsEffectPlaying { get; set; } // 엘리트 보상 timeSacle이 겹치지 않기 위해 추가
    public static bool IsLevelUpActive { get; set; } = false; // 레벨업 타이밍을 전달하기 위해 추가


    private void OpenMenu()
    {
        AudioManager.Play(SfxId.PopupOpen);   // SFX 가이드 아웃게임 8: 모든 팝업 등장(공용 단일 지점)
        IsMenuOpen = true;
        Time.timeScale = 0f;
    }

    private void CloseMenu()
    {
        if (IsEffectPlaying) return;

        if (IsAnyPopupActive()) return;

        IsMenuOpen = false;
        Time.timeScale = 1f;
    }

    // 씬 전환 완료 시 일시정지 상태 복구. 정산 재시도/다음, 로비 이동처럼 정지 상태 그대로
    // 씬을 떠나는 경로가 있어(전환 중 후면 전투 소리 방지), 새 씬이 뜨는 시점에 전역 리셋한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallSceneLoadReset()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (mode != LoadSceneMode.Single) return;
            IsMenuOpen = false;
            IsEffectPlaying = false;   // 연출 도중 씬 이탈 시 static 고착 방지 (엘리트 보상 연출 등)
            Time.timeScale = 1f;
        };
    }

    private bool IsAnyPopupActive()
    {
        return (_enchantSelectPopup.activeSelf ||
                _enchantChangePopup.activeSelf ||
                _enchantListPopup.activeSelf ||
                _optionPopup.activeSelf ||
                _settlementPopup.activeSelf ||
                _toLobbyPopup.activeSelf);
    }

    // ---------- UI 버튼 연동 ----------
    public void OnPauseButtonClick()
    {
        OpenMenu();
        ShowEnchantList();
    }

    public void OnCloseButtonClick()
    {
        HideEnchantSelection();
        HideEnchantChange();
        HideEnchantList();
        HideOption();
        CloseMenu();
    }

    public void OnCloseButtonClickInEnchantListPopup()
    {
        if (_enchantSelectPopup.activeInHierarchy)
        {
            HideEnchantList();
        }
        else
        {
            OnCloseButtonClick();
        }
    }

    // 로비로 돌아가기 경고창 > '예' 버튼 누를 때 동작 -> 로비로 이동
    public void ToLobbyAction()
    {
        if (_isLoadingLobby) return;

        _isLoadingLobby = true;
        // CloseMenu(일시정지 해제)를 부르지 않는다. 씬 전환 연출(약 2초) 동안 후면 전투가 재개되어
        // 이동/공격 소리가 나는 문제 방지. timeScale 복구는 아래 씬 로드 리셋이 맡는다.
        HideToLobby();
        HideOption();

        Debug.Log("[ScreenNavigator] 로비로 이동");
        OnLobbyClicked?.Invoke();
    }

    // ---------- 특수 웨이브 ----------
    private void ShowWavePopup(StageModel.SpawnType spawnType)
    {
        GameObject popupObject = null;
        if (spawnType == StageModel.SpawnType.Rush && _rushWavePopup != null)
        {
            popupObject = _rushWavePopup;
        }
        else if (spawnType == StageModel.SpawnType.Boss && _bossWavePopup != null)
        {
            popupObject = _bossWavePopup;
        }

        if (popupObject == null) return;

        popupObject.SetActive(true);
        ResetWavePopupTimer();
        _waveTimerActive = true;
    }

    private void HideWavePopup()
    {
        if (_bossWavePopup != null && _bossWavePopup.activeInHierarchy)
            _bossWavePopup.SetActive(false);
        if (_rushWavePopup != null && _rushWavePopup.activeInHierarchy)
            _rushWavePopup.SetActive(false);
        _waveTimerActive = false;
    }

    // ---------- 콤보 텍스트 ----------
    private void ShowComboTextPopup()
    {
        if (_comboTextPopup != null)
            _comboTextPopup.SetActive(true);
        ResetComboPopupTimer();
        _comboTimerActive = true;
    }

    private void HideComboTextPopup()
    {
        if (_comboTextPopup != null)
            _comboTextPopup.SetActive(false);
        _comboTimerActive = false;
    }
}
