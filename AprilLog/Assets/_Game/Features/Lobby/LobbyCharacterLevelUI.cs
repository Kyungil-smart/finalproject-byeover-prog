using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//작성자 : 홍정옥
// 기능 : 캐릭터 레벨업

public class LobbyCharacterLevelUI : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private PlayerProgressModel progressModel;
    [SerializeField] private CurrencyModel currencyModel;

    [Header("캐릭터 레벨 UI")]
    [SerializeField] private TextMeshProUGUI textCharCurrentLevel;
    [SerializeField] private Button btnLvUp;
    [SerializeField] private GameObject charNextLvInfo;

    [Header("현재 능력치")]
    [SerializeField] private TextMeshProUGUI textCurrentHP;
    [SerializeField] private TextMeshProUGUI textCurrentATK;
    [SerializeField] private TextMeshProUGUI textCurrentStern;
    [SerializeField] private TextMeshProUGUI textCurrentSlow;

    [Header("다음 레벨 능력치")]
    [SerializeField] private TextMeshProUGUI textNextHP;
    [SerializeField] private TextMeshProUGUI textNextATK;
    [SerializeField] private TextMeshProUGUI textNextStern;
    [SerializeField] private TextMeshProUGUI textNextSlow;

    [Header("능력치 아이콘")]
    [SerializeField] private Image iconCurrentHP;
    [SerializeField] private Image iconCurrentATK;
    [SerializeField] private Image iconCurrentStern;
    [SerializeField] private Image iconCurrentSlow;
    [SerializeField] private Image iconNextHP;
    [SerializeField] private Image iconNextATK;
    [SerializeField] private Image iconNextStern;
    [SerializeField] private Image iconNextSlow;

    [Header("레벨업 소모 재화")]
    [SerializeField] private TextMeshProUGUI textGoldCost;
    [SerializeField] private TextMeshProUGUI textParchmentCost;
    [SerializeField] private Color normalCostColor = Color.white;
    [SerializeField] private Color shortageCostColor = Color.red;

    [Header("팝업")]
    [SerializeField] private GameObject popupArea;
    [SerializeField] private TextMeshProUGUI textPopupMessage;
    [SerializeField] private float popupDuration = 1.5f;

    [Header("재화 감소 애니메이션")]
    [SerializeField] private float currencyAnimDuration = 0.6f;

    // ===== 임시 테스트용 (나중에 삭제) =====
    [Header("[임시] 테스트용 초기화")]
    [Tooltip("레벨/재화를 초기 상태로 되돌리는 테스트 버튼 (배포 전 삭제)")]
    [SerializeField] private Button btnReset;
    [Tooltip("초기화 시 복원할 골드")]
    [SerializeField] private int resetGold = CurrencyModel.TestStartGold;
    [Tooltip("초기화 시 복원할 양피지")]
    [SerializeField] private int resetParchment = CurrencyModel.TestStartParchment;
    [Tooltip("재화를 0으로 만드는 테스트 버튼 (배포 전 삭제)")]
    [SerializeField] private Button btnSetZero;
    [Tooltip("재화를 최대치로 만드는 테스트 버튼 (배포 전 삭제)")]
    [SerializeField] private Button btnSetMax;
    [Tooltip("최대치 버튼이 채울 골드")]
    [SerializeField] private int maxGold = 999999;
    [Tooltip("최대치 버튼이 채울 양피지")]
    [SerializeField] private int maxParchment = 999999;
    // =====================================

    private Coroutine _popupCoroutine;

    // 재화 애니메이션용 표시값 (실제 값과 별도 관리)
    private float _displayGold;
    private float _displayParchment;
    private Tween _goldTween;
    private Tween _parchmentTween;
    private bool  _isAnimatingCurrency;

    private void Awake()
    {
        AutoBindMissingReferences();

        if (popupArea != null)
            popupArea.SetActive(false);
    }

    private void OnEnable()
    {
        if (progressModel != null)
            progressModel.OnCharacterLevelChanged += HandleCharacterLevelChanged;

        if (currencyModel != null)
            currencyModel.OnCurrencyChanged += HandleCurrencyChanged;

        if (btnLvUp != null)
            btnLvUp.onClick.AddListener(TryLevelUp);

        // [임시] 초기화 버튼
        if (btnReset != null)
            btnReset.onClick.AddListener(ResetForTest);

        // [임시] 재화 0 버튼
        if (btnSetZero != null)
            btnSetZero.onClick.AddListener(SetCurrencyZeroForTest);

        // [임시] 재화 최대 버튼
        if (btnSetMax != null)
            btnSetMax.onClick.AddListener(SetCurrencyMaxForTest);

        Refresh();
    }

    private void OnDisable()
    {
        if (progressModel != null)
            progressModel.OnCharacterLevelChanged -= HandleCharacterLevelChanged;

        if (currencyModel != null)
            currencyModel.OnCurrencyChanged -= HandleCurrencyChanged;

        if (btnLvUp != null)
            btnLvUp.onClick.RemoveListener(TryLevelUp);

        // [임시] 초기화 버튼
        if (btnReset != null)
            btnReset.onClick.RemoveListener(ResetForTest);

        // [임시] 재화 0 버튼
        if (btnSetZero != null)
            btnSetZero.onClick.RemoveListener(SetCurrencyZeroForTest);

        // [임시] 재화 최대 버튼
        if (btnSetMax != null)
            btnSetMax.onClick.RemoveListener(SetCurrencyMaxForTest);
    }

    // ===== 임시 테스트용 (나중에 삭제) =====
    /// <summary>레벨을 1로, 재화를 초기값으로 되돌린다. (테스트 전용)</summary>
    private void ResetForTest()
    {
        // 진행 중인 재화 애니메이션 정리
        _goldTween?.Kill();
        _parchmentTween?.Kill();
        _isAnimatingCurrency = false;

        if (progressModel != null)
            progressModel.SetCharacterLevel(PlayerProgressModel.StartLevel);

        if (currencyModel != null)
            currencyModel.Initialize(resetGold, resetParchment);

        Refresh();
        ShowPopup("레벨/재화를 초기화했습니다.");
    }

    /// <summary>재화를 0으로 만든다. (재화 부족 상황 테스트용)</summary>
    private void SetCurrencyZeroForTest()
    {
        _goldTween?.Kill();
        _parchmentTween?.Kill();
        _isAnimatingCurrency = false;

        if (currencyModel != null)
            currencyModel.Initialize(0, 0);

        Refresh();
        ShowPopup("재화를 0으로 설정했습니다.");
    }

    /// <summary>재화를 최대치로 채운다. (테스트용)</summary>
    private void SetCurrencyMaxForTest()
    {
        _goldTween?.Kill();
        _parchmentTween?.Kill();
        _isAnimatingCurrency = false;

        if (currencyModel != null)
            currencyModel.Initialize(maxGold, maxParchment);

        Refresh();
        ShowPopup("재화를 최대치로 설정했습니다.");
    }
    // =====================================

    private void TryLevelUp()
    {
        if (progressModel == null || currencyModel == null)
        {
            ShowPopup("레벨업 데이터를 확인할 수 없습니다.");
            return;
        }

        int currentLevel = progressModel.CharacterLevel;
        if (currentLevel >= PlayerProgressModel.MaxLevel)
        {
            Refresh();
            ShowPopup("이미 최대 레벨입니다.");
            return;
        }

        CharacterLevelCostData cost = CharacterLevelData.GetLevelUpCost(currentLevel);
        if (!currencyModel.CanAfford(cost.Gold, cost.Parchment))
        {
            RefreshCostState(cost);
            ShowPopup("현재 재화가 부족합니다.");
            return;
        }

        // 소모 전 값 저장 → 애니메이션 시작점
        int oldGold      = currencyModel.Gold;
        int oldParchment = currencyModel.Parchment;

        if (!currencyModel.TrySpend(cost.Gold, cost.Parchment))
        {
            RefreshCostState(cost);
            ShowPopup("현재 재화가 부족합니다.");
            return;
        }

        progressModel.SetCharacterLevel(currentLevel + 1);

        // 레벨업 후 다음 레벨 소모량 계산 (or MAX)
        int nextLevel = progressModel.CharacterLevel;
        CharacterLevelCostData nextCost = nextLevel < PlayerProgressModel.MaxLevel
            ? CharacterLevelData.GetLevelUpCost(nextLevel)
            : new CharacterLevelCostData(0, 0);

        // 재화 감소 애니메이션 (보유량 파트만 카운트다운)
        AnimateCurrency(oldGold, currencyModel.Gold, oldParchment, currencyModel.Parchment, nextCost);

        // 스탯/레벨 텍스트는 즉시 업데이트
        RefreshStatAndLevel();
    }

    private void Refresh()
    {
        if (_isAnimatingCurrency) return;   // 애니메이션 중엔 재화 텍스트 덮어쓰지 않음

        if (progressModel == null)
            progressModel = FindFirstObjectByType<PlayerProgressModel>();

        if (currencyModel == null)
            currencyModel = FindFirstObjectByType<CurrencyModel>();

        RefreshStatAndLevel();

        int currentLevel = progressModel != null ? progressModel.CharacterLevel : PlayerProgressModel.StartLevel;
        bool isMaxLevel  = currentLevel >= PlayerProgressModel.MaxLevel;

        if (!isMaxLevel)
            RefreshCostState(CharacterLevelData.GetLevelUpCost(currentLevel));
    }

    /// <summary>스탯·레벨 텍스트만 즉시 갱신 (재화 애니메이션과 독립)</summary>
    private void RefreshStatAndLevel()
    {
        if (progressModel == null)
            progressModel = FindFirstObjectByType<PlayerProgressModel>();

        int currentLevel = progressModel != null ? progressModel.CharacterLevel : PlayerProgressModel.StartLevel;
        bool isMaxLevel  = currentLevel >= PlayerProgressModel.MaxLevel;

        SetText(textCharCurrentLevel, isMaxLevel ? "MAX" : $"Lv.{currentLevel:00}");

        CharacterStatData currentStat = CharacterLevelData.GetStat(currentLevel);
        SetStatTexts(currentStat, textCurrentHP, textCurrentATK, textCurrentStern, textCurrentSlow);

        if (charNextLvInfo != null)
            charNextLvInfo.SetActive(!isMaxLevel);

        if (!isMaxLevel)
        {
            CharacterStatData nextStat = CharacterLevelData.GetStat(currentLevel + 1);
            SetStatTexts(nextStat, textNextHP, textNextATK, textNextStern, textNextSlow);
        }
        else
        {
            SetText(textNextHP,    "MAX");
            SetText(textNextATK,   "MAX");
            SetText(textNextStern, "MAX");
            SetText(textNextSlow,  "MAX");
            SetText(textGoldCost,      "MAX");
            SetText(textParchmentCost, "MAX");
        }
    }

    private void RefreshCostState(CharacterLevelCostData cost)
    {
        int gold      = currencyModel != null ? currencyModel.Gold      : CurrencyModel.TestStartGold;
        int parchment = currencyModel != null ? currencyModel.Parchment : CurrencyModel.TestStartParchment;

        SetCostText(textGoldCost,      gold,      cost.Gold,      gold      >= cost.Gold);
        SetCostText(textParchmentCost, parchment, cost.Parchment, parchment >= cost.Parchment);
    }

    // ------------------------------------------------------------------
    // 재화 감소 카운트다운 애니메이션
    // ------------------------------------------------------------------
    private void AnimateCurrency(int fromGold, int toGold,
                                  int fromParchment, int toParchment,
                                  CharacterLevelCostData nextCost)
    {
        _isAnimatingCurrency = true;
        _displayGold      = fromGold;
        _displayParchment = fromParchment;

        _goldTween?.Kill();
        _parchmentTween?.Kill();

        bool goldDone      = false;
        bool parchmentDone = false;

        void CheckBothDone()
        {
            if (goldDone && parchmentDone)
            {
                _isAnimatingCurrency = false;
                // 애니메이션 끝나면 다음 레벨 소모량으로 최종 갱신
                if (nextCost.Gold > 0 || nextCost.Parchment > 0)
                    RefreshCostState(nextCost);
            }
        }

        _goldTween = DOTween.To(
            () => _displayGold,
            v  => {
                _displayGold = v;
                SetCostText(textGoldCost, Mathf.RoundToInt(v), nextCost.Gold, v >= nextCost.Gold);
            },
            toGold,
            currencyAnimDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => { goldDone = true; CheckBothDone(); });

        _parchmentTween = DOTween.To(
            () => _displayParchment,
            v  => {
                _displayParchment = v;
                SetCostText(textParchmentCost, Mathf.RoundToInt(v), nextCost.Parchment, v >= nextCost.Parchment);
            },
            toParchment,
            currencyAnimDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => { parchmentDone = true; CheckBothDone(); });
    }

    private void SetCostText(TextMeshProUGUI text, int owned, int cost, bool enough)
    {
        if (text == null)
            return;

        text.text = $"{CurrencyModel.FormatAmount(owned)} / {CurrencyModel.FormatAmount(cost)}";
        text.color = enough ? normalCostColor : shortageCostColor;
    }

    private void SetStatTexts(CharacterStatData stat, TextMeshProUGUI hp, TextMeshProUGUI atk, TextMeshProUGUI stern, TextMeshProUGUI slow)
    {
        SetText(hp, stat.HP.ToString());
        SetText(atk, stat.ATK.ToString());
        SetText(stern, stat.Stern.ToString());
        SetText(slow, stat.Slow.ToString());
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private void HandleCharacterLevelChanged(int level)
    {
        Refresh();
    }

    private void HandleCurrencyChanged(int gold, int parchment)
    {
        // TryLevelUp 내부에서 AnimateCurrency를 쓰므로 애니메이션 중엔 건너뜀
        if (!_isAnimatingCurrency)
            Refresh();
    }

    private void OnDestroy()
    {
        _goldTween?.Kill();
        _parchmentTween?.Kill();
        if (_popupCoroutine != null)
            StopCoroutine(_popupCoroutine);
    }

    private void ShowPopup(string message)
    {
        if (popupArea == null)
            return;

        if (textPopupMessage != null)
            textPopupMessage.text = message;

        if (_popupCoroutine != null)
            StopCoroutine(_popupCoroutine);

        _popupCoroutine = StartCoroutine(ShowPopupCoroutine());
    }

    private IEnumerator ShowPopupCoroutine()
    {
        popupArea.SetActive(true);
        yield return new WaitForSeconds(popupDuration);
        popupArea.SetActive(false);
        _popupCoroutine = null;
    }

    private void AutoBindMissingReferences()
    {
        if (progressModel == null)
            progressModel = FindFirstObjectByType<PlayerProgressModel>();

        if (currencyModel == null)
            currencyModel = FindFirstObjectByType<CurrencyModel>();

        textCharCurrentLevel = textCharCurrentLevel != null ? textCharCurrentLevel : FindText("Text_CharCurrentLevel");
        btnLvUp = btnLvUp != null ? btnLvUp : FindComponentByName<Button>("Btn_LvUp");
        btnReset = btnReset != null ? btnReset : FindComponentByName<Button>("Button_Reset"); // [임시] 테스트용
        btnSetZero = btnSetZero != null ? btnSetZero : FindComponentByName<Button>("Button_SetZero"); // [임시] 테스트용
        btnSetMax = btnSetMax != null ? btnSetMax : FindComponentByName<Button>("Button_SetMax"); // [임시] 테스트용
        charNextLvInfo = charNextLvInfo != null ? charNextLvInfo : FindGameObject("CharNextLvInfo");

        textCurrentHP = textCurrentHP != null ? textCurrentHP : FindText("Text_CurrentHP");
        textCurrentATK = textCurrentATK != null ? textCurrentATK : FindText("Text_CurrentATK");
        textCurrentStern = textCurrentStern != null ? textCurrentStern : FindText("Text_CurrentStern");
        textCurrentSlow = textCurrentSlow != null ? textCurrentSlow : FindText("Text_CurrentSlow");

        textNextHP = textNextHP != null ? textNextHP : FindText("Text_NextHP");
        textNextATK = textNextATK != null ? textNextATK : FindText("Text_NextATK");
        textNextStern = textNextStern != null ? textNextStern : FindText("Text_NextStern");
        textNextSlow = textNextSlow != null ? textNextSlow : FindText("Text_NextSlow");

        TextMeshProUGUI[] goldTexts = FindComponentsByName<TextMeshProUGUI>("Text_Gold");
        if (textGoldCost == null && goldTexts.Length > 1)
            textGoldCost = goldTexts[goldTexts.Length - 1];

        textParchmentCost = textParchmentCost != null ? textParchmentCost : FindText("Text_paper");
        popupArea = popupArea != null ? popupArea : FindGameObject("POPUPArea");
        textPopupMessage = textPopupMessage != null ? textPopupMessage : FindText("Text_POPUP");
    }

    private TextMeshProUGUI FindText(string objectName)
    {
        return FindComponentByName<TextMeshProUGUI>(objectName);
    }

    private GameObject FindGameObject(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform target in transforms)
        {
            if (target != null && target.name == objectName)
                return target.gameObject;
        }

        return null;
    }

    private T FindComponentByName<T>(string objectName) where T : Component
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform target in transforms)
        {
            if (target == null || target.name != objectName)
                continue;

            if (target.TryGetComponent(out T component))
                return component;
        }

        return null;
    }

    private T[] FindComponentsByName<T>(string objectName) where T : Component
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;

        foreach (Transform target in transforms)
        {
            if (target != null && target.name == objectName && target.TryGetComponent(out T _))
                count++;
        }

        T[] components = new T[count];
        int index = 0;

        foreach (Transform target in transforms)
        {
            if (target == null || target.name != objectName)
                continue;

            if (target.TryGetComponent(out T component))
                components[index++] = component;
        }

        return components;
    }
}
