// 담당자 : 김영찬
// WaveSystem MVP의 M 담당
// (구)StageRunner.cs에서 분화

using System;
using System.Collections.Generic;

/// <summary>
/// 유니티 엔진을 전혀 모르며 연산과 상태(State) 판단만 담당하는 최상위 핵심 두뇌 영역
/// SpawnTracker는 MonsterSpawner.cs에 정의되어 있음. 여기서 중복 선언 금지.
/// </summary>
public class StageModel
{
    private StageData _stageData;

    public StageModel(StageData stageData)
    {
        _stageData = stageData;
    }

    // ---------- 이벤트 ----------



    // ---------- Private ----------
    private int _currentWaveIndex;
    private int _totalWaveCount;
    private float _stageTimer;
    private float _stageTimeLimit;
    private int _thisWaveAliveMonster = 0;

    // ---------- 웨이브 제어 ----------
    public void StartWave()
    {



    }

    private void StartNextWave()
    {


    }

    public void SpawnMonsterCount()
    {
        _thisWaveAliveMonster++;
    }

    public void DeathMonsterCount(MonsterAI monster)
    {
        _thisWaveAliveMonster--;
    }

    // ---------- 타이머 관련 ----------
    public void GetCurrentTime(float curTime)
    {
        _stageTimer = curTime;
        if (_stageTimer >= _stageTimeLimit)
        {
            StartNextWave();
            GetStageTimeLimit();
        }
    }

    private void GetStageTimeLimit()
    {
        _stageTimeLimit = _stageData.TimeLimit;
    }
}