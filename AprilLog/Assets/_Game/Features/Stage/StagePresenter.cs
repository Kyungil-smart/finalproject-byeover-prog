// 담당자 : 김영찬
// Wave시스템 MVP 모델의 P
// (구)StageRunner.cs에서 분화

// 1차 수정자 : 정승우
// 수정내용 : WaveSpawner -> MonsterSpawner로 변경. 타이머는 StageLoopManager가 관리하므로 여기선 Model 갱신만.

using System;
using UnityEngine;

/// <summary>
/// StageModel과 MonsterSpawner(View)의 중재자
/// </summary>
public class WavePresenter : MonoBehaviour
{
    // ---------- 이벤트 ----------
    private event Action<float> _onStageTimeChange;

    // ---------- MVP 컴포넌트 ----------
    StageModel _model;

    [Header("MVP 컴포넌트 직렬화")]
    [SerializeField] MonsterSpawner _spawner;

    // ---------- 웨이브 공용 타이머 ----------
    public float StageTimer { get; private set; }

    // ---------- 이벤트 구독/해제 ----------
    private void OnEnable()
    {
        _onStageTimeChange += _model.GetCurrentTime;
        _spawner.OnMonsterDied += _model.DeathMonsterCount;
    }

    private void OnDisable()
    {
        _onStageTimeChange -= _model.GetCurrentTime;
        _spawner.OnMonsterDied -= _model.DeathMonsterCount;
    }

    private void Update()
    {
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        StageTimer += Time.deltaTime;
        _onStageTimeChange?.Invoke(StageTimer);
    }

    public void Release()
    {

    }
}