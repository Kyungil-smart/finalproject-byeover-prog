using TMPro;
using UnityEngine;

// 작성자 : 홍정옥
// 설명 : 상단바 정보 연동

public class LobbyTopBarUI : MonoBehaviour
{
    [Header("프로필 UI")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [Tooltip("상단바에 표시할 계정(플레이어) 레벨. 캐릭터 레벨과 별개로 관리한다.")]
    [SerializeField] private int accountLevel = 1;

    [Header("재화 UI")]
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text parchmentText;
    [Tooltip("다이아 표시 텍스트. (탑바의 양피지 자리를 다이아로 쓰려면 그 텍스트를 여기에 연결)")]
    [SerializeField] private TMP_Text diamondText;

    [Header("데이터")]
    [SerializeField] private PlayerProgressModel progressModel;
    [SerializeField] private CurrencyModel currencyModel;
    [SerializeField] private StaminaModel staminaModel;

    private const string DefaultNickname = "NICKNAME";

    private void Awake()
    {
        if (progressModel == null)
            progressModel = FindFirstObjectByType<PlayerProgressModel>();

        if (currencyModel == null)
            currencyModel = FindFirstObjectByType<CurrencyModel>();

        if (staminaModel == null)
            staminaModel = FindFirstObjectByType<StaminaModel>();
    }

    private void OnEnable()
    {
        // 캐릭터 레벨(progressModel.OnCharacterLevelChanged) 구독 제거 →
        // 상단바 레벨은 캐릭터 레벨업과 연동되지 않는다.
        if (currencyModel != null)
            currencyModel.OnCurrencyChanged += HandleCurrencyChanged;

        if (staminaModel != null)
            staminaModel.OnStaminaChanged += SetStamina;

        Refresh();
    }

    private void OnDisable()
    {
        if (currencyModel != null)
            currencyModel.OnCurrencyChanged -= HandleCurrencyChanged;

        if (staminaModel != null)
            staminaModel.OnStaminaChanged -= SetStamina;
    }

    public void Refresh()
    {
        SetNickname(GetNickname());
        SetLevel(accountLevel);   // 캐릭터 레벨이 아닌 계정 레벨 표시

        // 재화 (골드/양피지)
        if (currencyModel != null)
            HandleCurrencyChanged(currencyModel.Gold, currencyModel.Parchment);
        else
            HandleCurrencyChanged(CurrencyModel.TestStartGold, CurrencyModel.TestStartParchment);

        // 행동력 (현재/최대)
        if (staminaModel != null)
            SetStamina(staminaModel.Current, staminaModel.Max);
        else
            SetStamina(StaminaModel.TestStartStamina, StaminaModel.TestMaxStamina);
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

    /// <summary>행동력 표시 — "현재/최대" 형식 (예: 999/999)</summary>
    public void SetStamina(int current, int max)
    {
        if (staminaText != null)
            staminaText.text = $"{current}/{max}";
    }

    // 골드/양피지/다이아 갱신. 다이아도 CurrencyModel 로 통합되어 OnCurrencyChanged 에서 함께 갱신한다.
    private void HandleCurrencyChanged(int gold, int parchment)
    {
        if (goldText != null)
            goldText.text = CurrencyModel.FormatAmount(gold);

        if (parchmentText != null)
            parchmentText.text = CurrencyModel.FormatAmount(parchment);

        if (diamondText != null)
        {
            int diamond = currencyModel != null
                ? currencyModel.Diamond
                : (GameManager.Instance != null ? GameManager.Instance.Diamond : 0);
            diamondText.text = CurrencyModel.FormatAmount(diamond);
        }
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
