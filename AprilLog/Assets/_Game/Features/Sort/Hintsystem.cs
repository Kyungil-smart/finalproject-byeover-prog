// 담당자 : 정승우
// 설명   : 힌트 타이머 + 표시 대상 선정

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조작이 없으면 힌트를 보여준다.
/// HP 50% 이하면 대기 시간이 7초에서 4초로 줄어듦.
/// </summary>
public class HintSystem : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int, int> OnHintShow;   // tableIdx, slotIdx (이 유닛을 흔들어라)
    public event Action OnHintWaiting;          // 대기 테이블을 흔들어라

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private SortModel _model;
    [SerializeField] private PlayerModel _playerModel;

    [Header("설정")]
    [Tooltip("기본 힌트 대기 시간(초)")]
    [SerializeField] private float _normalDelay = 7f;

    [Tooltip("HP 50% 이하일 때 대기 시간(초)")]
    [SerializeField] private float _urgentDelay = 4f;

    // ---------- Private ----------
    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= GetDelay())
        {
            ShowHint();
            _timer = 0f;
        }
    }

    public void ResetTimer()
    {
        _timer = 0f;
    }

    private float GetDelay()
    {
        if (_playerModel == null) return _normalDelay;

        float hpRatio = (float)_playerModel.CurrentHP / Mathf.Max(1, _playerModel.MaxHP);
        return hpRatio <= 0.5f ? _urgentDelay : _normalDelay;
    }

    private void ShowHint()
    {
        var targets = _model.GetHintTargets();

        if (targets.Count > 0)
        {
            foreach (var t in targets)
            {
                OnHintShow?.Invoke(t.t, t.s);
            }
        }
        else
        {
            OnHintWaiting?.Invoke();
        }
    }
}