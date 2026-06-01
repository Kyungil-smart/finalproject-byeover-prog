using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private Coroutine _popupCoroutine;

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
    }

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

        if (!currencyModel.TrySpend(cost.Gold, cost.Parchment))
        {
            RefreshCostState(cost);
            ShowPopup("현재 재화가 부족합니다.");
            return;
        }

        progressModel.SetCharacterLevel(currentLevel + 1);
        Refresh();
    }

    private void Refresh()
    {
        if (progressModel == null)
            progressModel = FindFirstObjectByType<PlayerProgressModel>();

        if (currencyModel == null)
            currencyModel = FindFirstObjectByType<CurrencyModel>();

        int currentLevel = progressModel != null ? progressModel.CharacterLevel : PlayerProgressModel.StartLevel;
        bool isMaxLevel = currentLevel >= PlayerProgressModel.MaxLevel;

        SetText(textCharCurrentLevel, isMaxLevel ? "MAX" : $"Lv.{currentLevel:00}");

        CharacterStatData currentStat = CharacterLevelData.GetStat(currentLevel);
        SetStatTexts(currentStat, textCurrentHP, textCurrentATK, textCurrentStern, textCurrentSlow);

        if (charNextLvInfo != null)
            charNextLvInfo.SetActive(!isMaxLevel);

        if (!isMaxLevel)
        {
            CharacterStatData nextStat = CharacterLevelData.GetStat(currentLevel + 1);
            SetStatTexts(nextStat, textNextHP, textNextATK, textNextStern, textNextSlow);
            RefreshCostState(CharacterLevelData.GetLevelUpCost(currentLevel));
        }
        else
        {
            SetText(textNextHP, "MAX");
            SetText(textNextATK, "MAX");
            SetText(textNextStern, "MAX");
            SetText(textNextSlow, "MAX");
            SetText(textGoldCost, "MAX");
            SetText(textParchmentCost, "MAX");
        }
    }

    private void RefreshCostState(CharacterLevelCostData cost)
    {
        int gold = currencyModel != null ? currencyModel.Gold : CurrencyModel.TestStartGold;
        int parchment = currencyModel != null ? currencyModel.Parchment : CurrencyModel.TestStartParchment;

        SetCostText(textGoldCost, gold, cost.Gold, gold >= cost.Gold);
        SetCostText(textParchmentCost, parchment, cost.Parchment, parchment >= cost.Parchment);
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
        Refresh();
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
