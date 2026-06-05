// 담당자 : 정승우
// 설명   : 화면(팝업) 전환 전담 - SRP: 이것만 Show/Hide 한다

// 수정자 : 김영찬
// 수정내용 : 인게임 UI에 넘겨줄 정보 최신화

using UnityEngine;
using UnityEngine.Serialization;

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
    
    [Tooltip("콤보 텍스트 팝업")]
    [SerializeField] private GameObject _comboTextPopup;
    
    [Header("특수 웨이브 팝업 참조")]
    [SerializeField] private InGameHUDView _inGameHUDView;
    [SerializeField] private GameObject _rushWavePopup;
    [SerializeField] private GameObject _bossWavePopup;
    
    [Header("팝업 시간")] 
    [SerializeField][Range(0,10f)] private float _comboPopupTime = 1.5f;
    [SerializeField][Range(0,10f)] private float _specialWavePopupTime = 3f;

    // ---------- private ----------
    private float _comboPopupTimer;
    private float _wavePopupTimer;
    
    private bool _comboTimerActive;
    private bool _waveTimerActive;

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
        HideComboTextPopup();
        HideWavePopup();
        HideSettlement();
        CloseMenu();
    }
    
    // ---------- 시간 계산 ----------
    private void Update()
    {
        Tick(Time.deltaTime);
    }
    
    private void Tick(float deltaTime)
    {
        if(_comboTimerActive)
        {
            _comboPopupTimer += deltaTime;

            if (_comboPopupTimer >= _comboPopupTime)
            {
                HideComboTextPopup();
            }
        }
        if(_waveTimerActive)
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
    
    // ---------- 인챈트 선택 ----------
    public void ShowEnchantChange()
    {
        if (_enchantChangePopup != null)
            _enchantChangePopup.SetActive(true);
    }

    private void HideEnchantChange()
    {
        if (_enchantChangePopup != null)
            _enchantChangePopup.SetActive(false);
    }
    
    // ---------- 인챈트 선택 ----------
    public void ShowEnchantList()
    {
        HideOption();
        if (_enchantListPopup != null)
            _enchantListPopup.SetActive(true);
    }

    private void HideEnchantList()
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
    
    // ---------- 메뉴 개폐 ----------
    private void OpenMenu()
    {
        Time.timeScale = 0f;
    }

    private void CloseMenu()
    {
        Time.timeScale = 1f;
    }

    // ---------- 게임 일시 정지 / 해제 버튼 ----------
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
        
        if(popupObject == null) return;
        
        popupObject.SetActive(true);
        ResetWavePopupTimer();
        _waveTimerActive = true;
    }

    private void HideWavePopup()
    {
        if(_bossWavePopup != null && _bossWavePopup.activeInHierarchy)
            _bossWavePopup.SetActive(false);
        if(_rushWavePopup != null  && _rushWavePopup.activeInHierarchy)
            _rushWavePopup.SetActive(false);
        _waveTimerActive = false;
    }
    
    // ---------- 콤보 텍스트 ----------
    private void ShowComboTextPopup()
    {
        if(_comboTextPopup != null)
            _comboTextPopup.SetActive(true);
        ResetComboPopupTimer();
        _comboTimerActive = true;
    }

    private void HideComboTextPopup()
    {
        if(_comboTextPopup != null)
            _comboTextPopup.SetActive(false);
        _comboTimerActive = false;
    }
}
