using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TMPro;

/// <summary>
/// 기획팀 확률 검증용 테스트 씬 전용 스크립트 (TMP UI 제어형)
/// </summary>
public class StageDiceTester : MonoBehaviour
{
    [Header("UI 연결 (입력 및 출력)")]
    [Tooltip("검증해 볼 스테이지 ID를 입력받을 TMP InputField")]
    [SerializeField] private TMP_InputField _stageIdInput;
    
    [Tooltip("시뮬레이션 반복 횟수를 입력받을 TMP InputField")]
    [SerializeField] private TMP_InputField _runCountInput;

    [Tooltip("결과를 띄워줄 화면의 TMP Text 컴포넌트")]
    [SerializeField] private TextMeshProUGUI _resultText;

    private void Start()
    {
        // 시작할 때 기획자 편의를 위해 인풋필드에 기본값을 세팅해줍니다.
        if (_stageIdInput != null) _stageIdInput.text = "101";
        if (_runCountInput != null) _runCountInput.text = "10000";
    }
    
    public void OnClickRunSimulation()
    {
        // 입력창 데이터 안전하게 읽어오기
        if (!int.TryParse(_stageIdInput.text, out int targetStageId))
        {
            PrintToScreen("<color=red>[입력 오류]</color> 스테이지 ID 칸에 올바른 숫자를 입력해주세요.");
            return;
        }

        if (!int.TryParse(_runCountInput.text, out int runCount) || runCount <= 0)
        {
            PrintToScreen("<color=red>[입력 오류]</color> 시뮬레이션 횟수 칸에 1 이상의 숫자를 입력해주세요.");
            return;
        }

        // DB 체크
        if (DataManager.Instance == null || DataManager.Instance.StageRepo == null)
        {
            PrintToScreen("DataManager 또는 StageRepo가 초기화되지 않았습니다.\n씬에 DataManager 프리팹이 존재하는지 확인하세요.");
            return;
        }

        var rulesDict = DataManager.Instance.StageRepo.GetSpawnRulesForStage(targetStageId);
        if (rulesDict == null || rulesDict.Count == 0)
        {
            PrintToScreen($"<color=red>[데이터 없음]</color> 스테이지 {targetStageId}의 웨이브 룰 데이터를 찾을 수 없습니다.");
            return;
        }
        
        List<StageWaveRuleData> rules = rulesDict.Values.ToList();
        StageWaveRuleData testRule = rules[0];

        // 더미 모델 생성 및 리플렉션 세팅
        StageData dummyStageData = DataManager.Instance.StageRepo.GetStage(targetStageId) ?? new StageData { Stage_ID = targetStageId };
        StageModel dummyModel = new StageModel(dummyStageData, rules, new System.Random(), 0f);
        
        MethodInfo rollMethod = typeof(StageModel).GetMethod("RollDiceForMonster", BindingFlags.NonPublic | BindingFlags.Instance);

        if (rollMethod == null)
        {
            PrintToScreen("RollDiceForMonster 함수를 찾을 수 없습니다. StageModel을 확인하세요.");
            return;
        }

        // 화면 출력용 문자열 조립 시작
        string logMessage = $"<color=#00FFFF>[확률 검증 시작]</color> Stage: {targetStageId} / 횟수: {runCount}회\n\n";
        logMessage += $"<b>[기획 시트 세팅]</b>\nNormal {testRule.NormalChance}% | Agile {testRule.AgileChance}% | Tank {testRule.TankChance}% | Ranged {testRule.RangedChance}% | Infested {testRule.InfestedChance}%\n\n";

        Dictionary<int, int> resultCounts = new Dictionary<int, int>();

        // 시뮬레이션
        for (int i = 0; i < runCount; i++)
        {
            object resultObj = rollMethod.Invoke(dummyModel, new object[] { testRule, StageModel.SpawnType.Normal });

            StageModel.SpawnCommand cmd = (StageModel.SpawnCommand)resultObj;
            int characterId = cmd.CharacterId;

            if (!resultCounts.ContainsKey(characterId))
            {
                resultCounts[characterId] = 0;
            }
            resultCounts[characterId]++;
        }

        logMessage += "<color=#FFFF00>[시뮬레이션 결과]</color>\n";
        
        // 결과 정렬 및 출력
        foreach (var kvp in resultCounts.OrderByDescending(x => x.Value))
        {
            float percentage = (kvp.Value / (float)runCount) * 100f;
            string idString = kvp.Key == -1 ? "-1 (오류/빈칸)" : kvp.Key.ToString();
            
            logMessage += $"몬스터 ID [{idString}] : {kvp.Value}회 출현 <b>({percentage:F2}%)</b>\n";
        }

        PrintToScreen(logMessage);
    }

    private void PrintToScreen(string message)
    {
        Debug.Log(message); 
        
        if (_resultText != null)
        {
            _resultText.text = message; 
        }
    }
}