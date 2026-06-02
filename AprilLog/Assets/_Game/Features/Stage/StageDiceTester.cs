using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TMPro;

/// <summary>
/// 스테이지 별 몬스터 등장 확률 검증용 테스트 씬 전용 스크립트
/// </summary>
public class StageDiceTester : MonoBehaviour
{
    [Header("테스트 설정")]
    [Tooltip("검증해 볼 스테이지 ID (예: 101)")]
    [SerializeField] private int _testStageId = 101; 
    
    [Tooltip("시뮬레이션 반복 횟수")]
    [SerializeField] private int _runCount = 10000;

    [Header("UI 연결")]
    [Tooltip("결과를 띄워줄 화면의 Text 컴포넌트")]
    [SerializeField] private TextMeshProUGUI _resultText; // TextMeshProUGUI를 사용하신다면 타입을 변경해주세요.

    // 💡 UI Button의 OnClick 이벤트에 연결할 public 함수
    public void OnClickRunSimulation()
    {
        // 1. 매니저 접근 (없으면 에디터 기능으로 강제 로드됨)
        var dataManager = DataManager.Instance;
        if (dataManager == null)
        {
            PrintToScreen("DataManager를 생성할 수 없습니다.\n프리팹 경로를 확인해 주세요.");
            return;
        }

        // 💡 2. [핵심] 테스트 코드가 주도적으로 데이터 초기화를 강제 실행!
        // (DataManager 내부에 이중 호출 방지 처리가 되어 있으므로 몇 번을 눌러도 안전함)
        dataManager.InitRepo();

        // 3. 초기화를 했는데도 Repo가 없다면, 그건 프리팹 인스펙터 참조가 누락된 것(진짜 휴먼 에러)
        if (dataManager.StageRepo == null)
        {
            PrintToScreen("StageRepo가 DataManager에 연결되지 않았습니다.\nDataManager 프리팹의 Inspector 창을 확인하세요.");
            return;
        }

        // 4. 안전이 100% 보장된 상태로 데이터 로드 시작
        var rulesDict = dataManager.StageRepo.GetSpawnRulesForStage(_testStageId);
        if (rulesDict == null || rulesDict.Count == 0)
        {
            PrintToScreen($"스테이지 {_testStageId}의 웨이브 룰 데이터를 찾을 수 없습니다.");
            return;
        }
        
        List<StageWaveRuleData> rules = rulesDict.Values.ToList();
        StageWaveRuleData testRule = rules[0];
        SpecialWaveRuleData specialRule = DataManager.Instance.StageRepo.GetSpecialWaveRuleForStage(testRule.Stage_ID);

        StageModel dummyModel = new StageModel(new StageData(), rules, specialRule,new System.Random(), 0f);
        MethodInfo rollMethod = typeof(StageModel).GetMethod("RollDiceForMonster", BindingFlags.NonPublic | BindingFlags.Instance);

        if (rollMethod == null)
        {
            PrintToScreen("RollDiceForMonster 함수를 찾을 수 없습니다.");
            return;
        }

        // 화면에 출력할 결과 문자열 조립 시작
        string logMessage = $"<color=#00FFFF>[확률 검증 시작]</color> Stage: {_testStageId} / 횟수: {_runCount}회\n\n";
        logMessage += $"<b>[기획 시트 세팅]</b>\nNormal {testRule.NormalChance}% | Agile {testRule.AgileChance}% | Tank {testRule.TankChance}% | Ranged {testRule.RangedChance}% | Infested {testRule.InfestedChance}%\n\n";

        Dictionary<int, int> resultCounts = new Dictionary<int, int>();

        for (int i = 0; i < _runCount; i++)
        {
            int characterId = (int)rollMethod.Invoke(dummyModel, new object[] { testRule });

            if (!resultCounts.ContainsKey(characterId))
            {
                resultCounts[characterId] = 0;
            }
            resultCounts[characterId]++;
        }

        logMessage += "<color=#FFFF00>[시뮬레이션 결과]</color>\n";
        
        // 출현 횟수가 많은 순서대로 정렬해서 출력
        foreach (var kvp in resultCounts.OrderByDescending(x => x.Value))
        {
            float percentage = (kvp.Value / (float)_runCount) * 100f;
            logMessage += $"몬스터 ID [{kvp.Key}] : {kvp.Value}회 출현 <b>({percentage:F2}%)</b>\n";
        }

        // 최종 문자열을 UI와 콘솔에 동시 출력
        PrintToScreen(logMessage);
    }

    // 화면 UI와 콘솔에 동시에 로그를 찍어주는 편의성 함수
    private void PrintToScreen(string message)
    {
        Debug.Log(message); // 프로그래머용 콘솔 출력
        
        if (_resultText != null)
        {
            _resultText.text = message; // 기획자용 화면 출력
        }
    }
}