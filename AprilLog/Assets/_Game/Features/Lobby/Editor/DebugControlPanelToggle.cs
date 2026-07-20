using UnityEditor;
using UnityEditor.Build;
using System.Collections.Generic;

// 디버그 패널을 메뉴에서 원클릭으로 켜고 끈다. APRILOG_DEBUG 심볼을 현재 빌드 타깃에 넣거나 뺀다.
// 켜짐 = 팀원 테스트 빌드에 패널 포함, 꺼짐 = 스토어 빌드에서 코드 통째로 컴파일 아웃.
public static class DebugControlPanelToggle
{
    private const string Symbol = "APRILOG_DEBUG";
    private const string MenuPath = "Tools/디버그 패널 (APRILOG_DEBUG)";

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

        PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
        var list = new List<string>(defines);

        if (list.Contains(Symbol)) list.Remove(Symbol);
        else list.Add(Symbol);

        PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
    }

    [MenuItem(MenuPath, validate = true)]
    private static bool ToggleValidate()
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

        PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
        Menu.SetChecked(MenuPath, System.Array.IndexOf(defines, Symbol) >= 0);
        return true;
    }
}
