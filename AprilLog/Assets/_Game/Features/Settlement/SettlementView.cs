// 담당자 : 정승우
// 설명   : 정산 화면 View -- 결과, 통계, 보상 표시

// 수정자 : 정승우
// 수정내용 : UI 참조가 비어 있을 때 초기화를 건너뛰어 테스트 씬 NullReference 방지

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettlementView : MonoBehaviour, ISettlementView
{
    public event Action OnConfirmClicked;

    [Header("UI")]
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private TMP_Text _comboText;
    [SerializeField] private TMP_Text _damageText;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _parchmentText;
    [SerializeField] private Button _confirmButton;

    private SettlementPresenter _presenter;
    private bool _isInitialized;

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            if (_confirmButton == null)
            {
                Debug.LogWarning("[SettlementView] Confirm Button 참조가 비어 있어 초기화를 건너뜁니다.", this);
                return;
            }

            _isInitialized = true;
            _presenter = new SettlementPresenter(this);
            _confirmButton.onClick.AddListener(() => OnConfirmClicked?.Invoke());
        }
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
    public void SetResult(bool isVictory)
    {
        if (_resultText != null)
            _resultText.text = isVictory ? "Victory" : "Defeat";
    }

    public void SetStats(int maxCombo, int totalDamage)
    {
        if (_comboText != null)
            _comboText.SetText("{0}", maxCombo);
        if (_damageText != null)
            _damageText.SetText("{0}", totalDamage);
    }

    public void SetRewards(int gold, int parchment)
    {
        if (_goldText != null)
            _goldText.SetText("{0}", gold);
        if (_parchmentText != null)
            _parchmentText.SetText("{0}", parchment);
    }
}
