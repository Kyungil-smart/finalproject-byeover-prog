using System.Collections.Generic;
using UnityEngine;

public class UnitDataManager : MonoBehaviour
{
    [SerializeField] private UnitMasterTable _unitMasterTable;

    private Dictionary<int, UnitTableData> _unitDataMap;

    private static readonly Dictionary<int, int> TypeToIdMap = new Dictionary<int, int>()
{
    { 0, 1001 }, // Red
    { 1, 1002 }, // Blue
    { 2, 1004 }, // Green
    { 3, 1003 }, // Yellow
    { 4, 1005 }, // White
    { 5, 1006 }  // Joker
};

    private void Awake()
    {
        _unitDataMap = new Dictionary<int, UnitTableData>();

        foreach (var data in _unitMasterTable.rows)
        {
            _unitDataMap[data.UnitID] = data;
            string path = $"Units/{data.Unit_Image_ID}";
            data.UnitSprite = Resources.Load<Sprite>(path);

            if (data.UnitSprite == null)
            {
                Debug.LogWarning($"[UnitDataManager] 경로 {path}에서 스프라이트를 찾을 수 없습니다.");
            }
        }
    }

    // UnitType(Red0/Blue1/Green2/Yellow3/Purple4)을 UnitMasterTable의 UnitID로 매핑.
    // UnitMasterTable이 1003=노랑(yellowbook)/1004=초록(greenbook) 순서라 enum의 Green(2)/Yellow(3)와 어긋남.
    //   선형(1000+type+1)은 초록·노랑 타일의 표시 스프라이트를 서로 뒤바꿔, 조합 레시피가 카드 색과 다른 색에 발동하던 버그를 만들었다(2026-06-25 QA #181).
    //   → Green(2)=1004, Yellow(3)=1003으로 명시 매핑해 표시색을 enum·레시피·카드와 일치시킨다. (조합 9종 일괄 정상화)
    private static readonly int[] UnitTypeToTableId = { 1001, 1002, 1004, 1003, 1005 }; // Red/Blue/Green/Yellow/Purple(White)

    public UnitTableData GetUnitData(int unitType)
    {
        if (unitType < 0) return null;

        if (!TypeToIdMap.TryGetValue(unitType, out int realId))
        {
            realId = 1000 + (unitType + 1);
        }

        if (_unitDataMap.TryGetValue(realId, out var data))
        {
            return data;
        }

        Debug.LogWarning($"[UnitDataManager] {realId} 번 데이터를 찾을 수 없습니다!");
        return null;
    }
}
