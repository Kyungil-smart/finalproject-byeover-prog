// 담당자 : 정승우
// 설명   : 캐릭터 성장 View -- 레벨업 UI

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
    [SerializeField] private ConfigRepo _configRepo;

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
            _presenter = new GrowthPresenter(this, _growthSystem, _currency, _progress, _configRepo);
            _levelUpButton.onClick.AddListener(() => OnLevelUpClicked?.Invoke());
            _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
        }
        _presenter?.Refresh();
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void SetCurrentLevel(int level) => _levelText.SetText("Lv.{0}", level);
    public void SetRequiredResources(int gold, int parchment)
    {
        _costGoldText.SetText("{0}", gold);
        _costParchmentText.SetText("{0}", parchment);
    }
    public void SetCurrentResources(int gold, int parchment) { /* 보유량 표시 */ }
    public void EnableLevelUpButton(bool canAfford) => _levelUpButton.interactable = canAfford;
    public void PlayLevelUpEffect() { /* 연출 */ }
}
