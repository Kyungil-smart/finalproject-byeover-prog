// 담당자 : 정승우
// 설명   : 콤보 Model -- 카운트, 타이머, 코요테 타임

using System;
using UnityEngine;

/// <summary>
/// 콤보 수치와 타이머를 관리한다.
/// 기획서: 5초(+코요테 0.5초) 제한, 콤보당 보너스 데미지.
/// </summary>
public class ComboModel : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnComboChanged;              // count
    public event Action<float> OnComboTimerChanged;       // 남은 비율 0~1 (UI용)
    public event Action OnComboReset;

    // ---------- SerializeField ----------
    [Header("콤보 설정")]
    [Tooltip("유저에게 보여주는 제한 시간(초)")]
    [SerializeField] private float _visibleTimeLimit = 5.0f;

    [Tooltip("숨겨진 코요테 타임(초). 유저한테 안 보이는 여유 시간")]
    [SerializeField] private float _coyoteTime = 0.5f;

    [Tooltip("콤보 보너스 적용 최대 콤보 수")]
    [SerializeField] private int _maxBonusCombo = 10;

    [Tooltip("콤보 1개당 추가 데미지")]
    [SerializeField] private int _bonusPerCombo = 10;

    // ---------- 데이터 ----------
    public int CurrentCombo { get; private set; }
    public int MaxComboThisRun { get; private set; }

    private float _timer;
    private bool _isActive;

    // ---------- Update ----------
    private void Update()
    {
        if (!_isActive) return;

        _timer -= Time.deltaTime;

        // UI에는 코요테 타임 빼고 보여줌
        float visibleRemain = Mathf.Max(0f, _timer - _coyoteTime);
        float ratio = visibleRemain / _visibleTimeLimit;
        OnComboTimerChanged?.Invoke(ratio);

        // 진짜 만료는 코요테 타임까지 포함
        if (_timer <= 0f)
        {
            ResetCombo();
        }
    }

    // ---------- 조작 ----------
    public void IncrementCombo()
    {
        CurrentCombo++;
        _timer = _visibleTimeLimit + _coyoteTime;
        _isActive = true;

        if (CurrentCombo > MaxComboThisRun)
            MaxComboThisRun = CurrentCombo;

        OnComboChanged?.Invoke(CurrentCombo);
    }

    public void ResetCombo()
    {
        CurrentCombo = 0;
        _isActive = false;
        OnComboReset?.Invoke();
        OnComboChanged?.Invoke(0);
    }

    public int GetComboBonus()
    {
        int effective = Mathf.Min(CurrentCombo, _maxBonusCombo);
        return effective * _bonusPerCombo;
    }

    public void ResetForNewChapter()
    {
        CurrentCombo = 0;
        MaxComboThisRun = 0;
        _isActive = false;
    }
}