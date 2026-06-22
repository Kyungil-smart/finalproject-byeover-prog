using System.Collections.Generic;
using UnityEngine;

public class UnitDataManager : MonoBehaviour
{
    [SerializeField] private UnitMasterTable _unitMasterTable;

    private Dictionary<int, UnitTableData> _unitDataMap;

    private void Awake()
    {
        _unitDataMap = new Dictionary<int, UnitTableData>();
        foreach (var data in _unitMasterTable.rows)
        {
            _unitDataMap[data.UnitID] = data;
        }
    }

    public UnitTableData GetUnitData(int unitType)
    {
        int realId = 1000 + (unitType + 1);

        if (_unitDataMap.TryGetValue(realId, out var data))
        {
            return data;
        }
        Debug.Log($"{realId} 번 데이터를 찾을 수 없습니다!");
        return null;
    }
}
