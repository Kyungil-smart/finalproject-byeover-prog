// 담당자 : 정승우
// 설명   : 챕터/스테이지/웨이브 데이터 저장소

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 스테이지 진행 관련 SO를 Dictionary로 캐싱한다.
/// StageMonster는 1:N이라 Dictionary<int, List> 형태.
/// </summary>
public class StageRepo : MonoBehaviour
{
    [Header("SO 참조")]
    [SerializeField] private ChapterTable _chapterTable;
    [SerializeField] private StageDataTable _stageTable;
    [SerializeField] private StageMonsterTable _stageMonsterTable;
    [SerializeField] private MonsterScalingTable _monsterScalingTable;

    private Dictionary<int, ChapterData> _chapters;
    private Dictionary<int, StageData> _stages;
    private Dictionary<int, List<StageMonsterData>> _stageMonsters;
    private Dictionary<int, MonsterScalingData> _scaling;

    public void Initialize()
    {
        _chapters = _chapterTable.rows.ToDictionary(r => r.ChapterID);
        _stages = _stageTable.rows.ToDictionary(r => r.StageID);
        _scaling = _monsterScalingTable.rows.ToDictionary(r => r.StageID);

        // StageMonster는 StageID 기준으로 그룹핑
        _stageMonsters = new Dictionary<int, List<StageMonsterData>>();
        for (int i = 0; i < _stageMonsterTable.rows.Count; i++)
        {
            var row = _stageMonsterTable.rows[i];
            if (!_stageMonsters.ContainsKey(row.StageID))
                _stageMonsters[row.StageID] = new List<StageMonsterData>();
            _stageMonsters[row.StageID].Add(row);
        }

        Debug.Log($"[StageRepo] 초기화 완료. 챕터 {_chapters.Count}, 스테이지 {_stages.Count}");
    }

    public ChapterData GetChapter(int id) => _chapters[id];
    public StageData GetStage(int id) => _stages[id];
    public List<StageMonsterData> GetStageMonsters(int stageId)
        => _stageMonsters.TryGetValue(stageId, out var list) ? list : new List<StageMonsterData>();
    public MonsterScalingData GetMonsterScaling(int stageId)
        => _scaling.TryGetValue(stageId, out var data) ? data : null;
}
