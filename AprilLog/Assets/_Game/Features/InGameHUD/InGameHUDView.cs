// 담당자 : 정승우
// 설명   : 인게임 HUD View -- HP, EXP, 콤보, 진행도 표시

// 수정자 : Codex
// 수정내용 : Model 참조가 비어 있을 때 Presenter 생성을 건너뛰어 테스트 씬 NullReference 방지

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
        if (_playerModel == null || _comboModel == null)
        {
            Debug.LogWarning("[InGameHUDView] PlayerModel 또는 ComboModel 참조가 비어 있어 초기화를 건너뜁니다.", this);
            return;
        }

        _presenter = new InGameHUDPresenter(this, _playerModel, _comboModel);
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
    }

    // ---------- IInGameHUDView ----------
    public void UpdateHP(float ratio)
    {
        if (_hpSlider != null)
            _hpSlider.value = ratio;
    }

    public void UpdateShield(float ratio)
    {
        if (_shieldSlider != null) _shieldSlider.value = ratio;
    }

    public void UpdateEXP(float ratio)
    {
        if (_expSlider != null)
            _expSlider.value = ratio;
    }

    public void UpdateCombo(int count)
    {
        if (_comboText != null)
            _comboText.SetText("{0}", count);
    }

    public void UpdateComboTimer(float remainRatio)
    {
        if (_comboTimerSlider != null) _comboTimerSlider.value = remainRatio;
    }

    public void UpdateStageProgress(float ratio)
    {
        if (_stageProgressSlider != null)
            _stageProgressSlider.value = ratio;
    }

    public void ShowLevelUpEffect() { /* DOTween 연출 넣을 자리 */ }
}
