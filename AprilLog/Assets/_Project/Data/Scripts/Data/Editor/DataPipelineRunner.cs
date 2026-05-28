// 담당자 : 김영찬
// 설명   : 엑셀 데이터 전체 자동화 실행 메뉴
// 수정자 : 정승우
// 수정내용 : 엑셀 변환, 테이블 생성, SO Import를 한 메뉴에서 실행

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DataPipelineRunner
{
    [MenuItem("Tools/Data/Run Full Pipeline")]
    public static void RunFullPipeline()
    {
        Debug.Log("===========================================");
        Debug.Log("[DataPipeline] 전체 자동화 시작");
        Debug.Log("  1. Excel -> JSON");
        Debug.Log("  2. SO Table Script 확인/생성");
        Debug.Log("  3. JSON -> SO");
        Debug.Log("===========================================");

        ExcelToJsonConverter.ConvertAll();
        DataTableGenerator.GenerateAll();
        DataImporter.ImportAll();

        Debug.Log("[DataPipeline] 전체 자동화 요청 완료. 새 C# 파일이 생성된 경우 Unity 컴파일 후 다시 실행하세요.");
    }
}
#endif
