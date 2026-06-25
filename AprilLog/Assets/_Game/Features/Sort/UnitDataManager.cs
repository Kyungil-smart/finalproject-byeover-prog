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
            string path = $"Units/{data.Unit_Image_ID}";
            data.UnitSprite = Resources.Load<Sprite>(path);

            if (data.UnitSprite == null)
            {
                Debug.LogWarning($"[UnitDataManager] 경로 {path}에서 스프라이트를 찾을 수 없습니다.");
            }
        }
    }

    public UnitTableData GetUnitData(int unitType)
    {
        if (_unitDataMap == null) Awake();

        UnitTableData data = null;

        if (unitType == 5)
        {
            if (_unitDataMap.TryGetValue(1006, out data))
            {
                return data;
            }
        }

        int realId = 1000 + (unitType + 1);

        if (_unitDataMap.TryGetValue(realId, out data))
        {
            return data;
        }

        Debug.Log($"{realId} 번 데이터를 찾을 수 없습니다!");
        return null;
    }
}
