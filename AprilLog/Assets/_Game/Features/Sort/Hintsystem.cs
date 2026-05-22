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
        var counts = _model.CountUnitTypes();

        // 3개 이상인 유닛 찾기
        var candidates = new List<int>();
        foreach (var pair in counts)
        {
            if (pair.Value >= 3)
                candidates.Add(pair.Key);
        }

        if (candidates.Count > 0)
        {
            // 그 유닛이 있는 슬롯 중 하나를 흔들기
            int hintUnit = candidates[UnityEngine.Random.Range(0, candidates.Count)];

            for (int t = 0; t < SortModel.TABLE_COUNT; t++)
            {
                for (int s = 0; s < SortModel.SLOTS_PER_TABLE; s++)
                {
                    if (_model.GetUnit(t, s) == hintUnit)
                    {
                        OnHintShow?.Invoke(t, s);
                        return;
                    }
                }
            }
        }
        else
        {
            // 3개 이상인 게 없으면 대기 테이블 흔들기
            OnHintWaiting?.Invoke();
        }
    }
}