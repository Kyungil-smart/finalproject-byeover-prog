using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JokerPatternData", menuName = "Scriptable Objects/JokerPatternData")]
public class JokerPatternData : ScriptableObject
{
    [Tooltip("조커 유닛 작동 시 타겟이 될 테이블 인덱스 순서")]
    public List<int> tableIndices;
}
