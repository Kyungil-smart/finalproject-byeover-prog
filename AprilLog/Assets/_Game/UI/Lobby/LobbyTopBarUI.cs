using TMPro;
using UnityEngine;

// 작성자 : 홍정옥
// 설명 : 상단바 정보 연동

public class LobbyTopBarUI : MonoBehaviour
{
    [Header("프로필 UI")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;

    [Header("재화 UI")]
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text parchmentText;

    [Header("데이터")]
    [SerializeField] private PlayerProgressModel progressModel;
    [SerializeField] private CurrencyModel currencyModel;

    private const string DefaultNickname = "NICKNAME";

    private void Awake()
    {
        if (progressModel == null)
            progressModel = FindFirstObjectByType<PlayerProgressModel>();

        if (currencyModel == null)
            currencyModel = FindFirstObjectByType<CurrencyModel>();
    }

    private void OnEnable()
    {
        if (progressModel != null)
            progressModel.OnCharacterLevelChanged += SetLevel;

        if (currencyModel != null)
            currencyModel.OnTestCurrencyChanged += HandleCurrencyChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (progressModel != null)
            progressModel.OnCharacterLevelChanged -= SetLevel;

        if (currencyModel != null)
            currencyModel.OnTestCurrencyChanged -= HandleCurrencyChanged;
    }

    public void Refresh()
    {
        SetNickname(GetNickname());
        SetLevel(progressModel != null ? progressModel.CharacterLevel : PlayerProgressModel.StartLevel);

        if (currencyModel != null)
            HandleCurrencyChanged(currencyModel.Gold, currencyModel.ActionPoint, currencyModel.MaxActionPoint, currencyModel.Parchment);
        else
            HandleCurrencyChanged(CurrencyModel.TestStartGold, CurrencyModel.TestStartActionPoint, CurrencyModel.TestMaxActionPoint, CurrencyModel.TestStartParchment);
    }

    public void SetNickname(string nickname)
    {
        if (nicknameText == null)
            return;

        nicknameText.text = string.IsNullOrWhiteSpace(nickname) ? DefaultNickname : nickname;
    }

    public void SetLevel(int level)
    {
        if (levelText == null)
            return;

        levelText.text = $"LV. {Mathf.Max(PlayerProgressModel.StartLevel, level)}";
    }

    public void SetCurrency(int stamina, int gold)
    {
        if (staminaText != null)
            staminaText.text = CurrencyModel.FormatAmount(stamina);

        if (goldText != null)
            goldText.text = CurrencyModel.FormatAmount(gold);
    }

    private void HandleCurrencyChanged(int gold, int actionPoint, int maxActionPoint, int parchment)
    {
        if (goldText != null)
            goldText.text = CurrencyModel.FormatAmount(gold);

        if (staminaText != null)
            staminaText.text = CurrencyModel.FormatAmount(actionPoint);

        if (parchmentText != null)
            parchmentText.text = CurrencyModel.FormatAmount(parchment);
    }

    private string GetNickname()
    {
        UserCloudData cloudData = GameManager.Instance != null ? GameManager.Instance.CloudData : null;
        if (cloudData == null)
            return DefaultNickname;

        if (!string.IsNullOrWhiteSpace(cloudData.playerId))
            return cloudData.playerId;

        if (!string.IsNullOrWhiteSpace(cloudData.displayName))
            return cloudData.displayName;

        return DefaultNickname;
    }
}
