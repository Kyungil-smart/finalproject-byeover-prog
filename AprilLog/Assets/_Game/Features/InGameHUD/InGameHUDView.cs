// 담당자 : 정승우
// 설명   : 인게임 HUD View -- HP, EXP, 콤보, 진행도 표시

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인게임 HUD 화면 표시 담당. 로직 없음.
/// </summary>
public class InGameHUDView : MonoBehaviour, IInGameHUDView
{
    // ---------- SerializeField ----------
    [Header("데이터 참조")]
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private ComboModel _comboModel;

    [Header("UI")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private Slider _shieldSlider;
    [SerializeField] private Slider _expSlider;
    [SerializeField] private TMP_Text _comboText;
    [SerializeField] private Slider _comboTimerSlider;
    [SerializeField] private Slider _stageProgressSlider;

    // ---------- Private ----------
    private InGameHUDPresenter _presenter;

    // ---------- 생명주기 ----------
    private void Awake()
    {
        _presenter = new InGameHUDPresenter(this, _playerModel, _comboModel);
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
    }

    // ---------- IInGameHUDView ----------
    public void UpdateHP(float ratio) => _hpSlider.value = ratio;
    public void UpdateShield(float ratio)
    {
        if (_shieldSlider != null) _shieldSlider.value = ratio;
    }
    public void UpdateEXP(float ratio) => _expSlider.value = ratio;
    public void UpdateCombo(int count) => _comboText.SetText("{0}", count);
    public void UpdateComboTimer(float remainRatio)
    {
        if (_comboTimerSlider != null) _comboTimerSlider.value = remainRatio;
    }
    public void UpdateStageProgress(float ratio) => _stageProgressSlider.value = ratio;
    public void ShowLevelUpEffect() { /* DOTween 연출 넣을 자리 */ }
}
