// 담당자 : 정승우
// 설명   : 캐릭터 성장 View -- 레벨업 UI

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GrowthView : MonoBehaviour, IGrowthView
{
    public event Action OnLevelUpClicked;
    public event Action OnCloseClicked;

    [Header("참조")]
    [SerializeField] private OutGameGrowthSystem _growthSystem;
    [SerializeField] private CurrencyModel _currency;
    [SerializeField] private PlayerProgressModel _progress;

    [Header("UI")]
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _costGoldText;
    [SerializeField] private TMP_Text _costParchmentText;
    [SerializeField] private Button _levelUpButton;
    [SerializeField] private Button _closeButton;

    private GrowthPresenter _presenter;
    private bool _isInitialized;

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            DataManager dataManager = DataManager.Instance;
            ConfigRepo configRepo = dataManager != null ? dataManager.ConfigRepo : null;

            if (_growthSystem == null || _currency == null || _progress == null || configRepo == null)
            {
                Debug.LogWarning("[GrowthView] Required dependency is missing. GrowthPresenter creation skipped.");
                return;
            }

            _presenter = new GrowthPresenter(this, _growthSystem, _currency, _progress, configRepo);

            if (_levelUpButton != null)
                _levelUpButton.onClick.AddListener(() => OnLevelUpClicked?.Invoke());
            else
                Debug.LogWarning("[GrowthView] LevelUp button is not assigned.");

            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            else
                Debug.LogWarning("[GrowthView] Close button is not assigned.");
        }
        _presenter?.Refresh();
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void SetCurrentLevel(int level)
    {
        if (_levelText == null)
        {
            Debug.LogWarning("[GrowthView] Level text is not assigned.");
            return;
        }

        _levelText.SetText("Lv.{0}", level);
    }
    public void SetRequiredResources(int gold, int parchment)
    {
        if (_costGoldText != null)
            _costGoldText.SetText("{0}", gold);
        else
            Debug.LogWarning("[GrowthView] Cost gold text is not assigned.");

        if (_costParchmentText != null)
            _costParchmentText.SetText("{0}", parchment);
        else
            Debug.LogWarning("[GrowthView] Cost parchment text is not assigned.");
    }
    public void SetCurrentResources(int gold, int parchment) { /* 보유량 표시 */ }
    public void EnableLevelUpButton(bool canAfford)
    {
        if (_levelUpButton == null)
        {
            Debug.LogWarning("[GrowthView] LevelUp button is not assigned.");
            return;
        }

        _levelUpButton.interactable = canAfford;
    }
    public void PlayLevelUpEffect() { /* 연출 */ }
}
