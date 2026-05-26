// 담당자 : 김영찬
// WaveSystem MVP의 M 담당
// (구)StageRunner.sc에서 분화

using System;
using System.Collections.Generic;

/// <summary>
/// 유니티 엔진을 전혀 모르며 연산과 상태(State) 판단만 담당하는 최상위 핵심 두뇌 영역
/// </summary>
public class WaveModel
{
    private StageData _stageData;
    
    public WaveModel(StageData stageData)
    {
        _stageData = stageData;
    }
    
    // ---------- 이벤트 ----------
    public event Action<List<StageMonsterData>> OnStageDataSet;
    
    
    // ---------- Private ----------
    private List<StageMonsterData> _waveMonsters;
    private int _currentWaveIndex;
    private int _totalWaveCount;
    private float _stageTimer;
    private float _stageTimeLimit;
    private int _thisWaveAliveMonster = 0;

    // ---------- 웨이브 제어 ----------
    public void StartWave(List<StageMonsterData> allMonsters, int waveIndex)
    {
        _waveMonsters = new List<StageMonsterData>();
        for (int i = 0; i < allMonsters.Count; i++)
        {
            if (allMonsters[i].WaveIndex == waveIndex)
            {
                _waveMonsters.Add(allMonsters[i]);
            }
        }
        
        OnStageDataSet?.Invoke(_waveMonsters);
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
