// 담당자 : 정승우
// 설명   : 캔버스 스케일 정책 통일 -- 태블릿 등 넓은 화면에서 UI 잘림/겹침 방지.
//
// 배경: 씬마다 CanvasScaler 설정이 제각각(Match 0/0.5 혼재)이었다. 기준 해상도 1440x3120은
// 좁은 폰(비율 0.46) 기준이라, 그보다 훨씬 넓은 태블릿(Galaxy Tab A9+ 세로 = 0.625)에서
// Match 0(폭 기준) 캔버스는 UI가 폭에 맞춰 커지며 세로 공간이 모자라 잘리고 겹쳤다.
//
// 해법: ScaleWithScreenSize 캔버스의 ScreenMatchMode를 Expand로 통일한다.
// Expand = 기준 해상도 전체가 어떤 비율에서도 잘리지 않는 배율(폭/높이 중 작은 쪽) 사용.
// 태블릿에서는 UI가 약간 작아지고 여백이 생기지만 절대 잘리지 않는다(세로 게임 + 태블릿의 표준).
//
// 씬 파일을 고치는 대신 씬 로드 때 런타임으로 정규화한다 -- 21개+ 캔버스 일괄 적용,
// 새 씬/캔버스가 추가돼도 자동 커버, 씬 머지 충돌 없음. World Space/Constant Pixel 캔버스는 건드리지 않는다.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CanvasScalerNormalizer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        Normalize();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Normalize();
    }

    private static void Normalize()
    {
        foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
            if (scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand) continue;

            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
    }
}
