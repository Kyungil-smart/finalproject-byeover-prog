// 작성자 : 김영찬
// 클라우드 데이터가 로딩 될 동안 임시로 시나리오를 저장 해두었다가 클라우드 데이터가 로딩되면 기존의 시나리오를 저장

using System.Collections.Generic;
using UnityEngine;

public static class LoadedScenarioTempContainer
{
    private static Queue<int> _unsavedFirstReadScenario = new ();
    
    public static void UnsavedFirstReadScenarioResister(int groupId)
    {
        _unsavedFirstReadScenario ??= new Queue<int>();
        _unsavedFirstReadScenario.Enqueue(groupId);
        Debug.Log($"[LoadedScenarioTempContainer] 시나리오 해금 임시 저장({groupId})");
    }

    public static void SaveContainScenario()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LoadedScenarioTempContainer] GamaManager 미 구동으로 저장되지 않음.");
            return;
        }

        while (_unsavedFirstReadScenario.Count > 0)
        {
            int groupId = _unsavedFirstReadScenario.Dequeue();
            GameManager.Instance.SaveFirstReadScenario(groupId);
        }
        Debug.Log($"[LoadedScenarioTempContainer] 임시 저장된 시나리오 해금 정보 클라우드 데이터에 저장");
    }
}
