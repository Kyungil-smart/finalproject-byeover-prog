using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StateFeedBackColorSO", menuName = "Scriptable Objects/StateFeedBackColorSO")]
public class StateFeedBackColorSO : ScriptableObject
{
    // ---------- SerializeField ----------
    [Header("피격 시 컬러 설정")]
    [SerializeField] Color _onHitColor = new Color(255, 0, 0, 150);              // 빨간색
    
    [Header("상태이상 시 컬러 설정")]
    [SerializeField] List<EffectColorMapping> _effectColorMappings = new List<EffectColorMapping>();
    
    [Header("CC 연출 우선순위 (Index 0에 가까울 수록 우선)")]
    [SerializeField] private List<CrowdControlType> _ccPriorityList = new List<CrowdControlType>();

    // ---------- Dictionary ----------
    private Dictionary<CrowdControlType, Color> _effectColorDictionary;
    
    // ---------- Initialize ----------
    private void Awake()
    {
        _effectColorDictionary = BuildDictionary();
    }
    
    // ---------- 데이터 검증 (중복 확인용) ----------
    private void OnValidate()
    {
        ValidateData();
    }
    
    // ---------- 조회 API ----------
    public Color GetOnHitColor() => _onHitColor;
    public List<CrowdControlType> CCPriorityList => _ccPriorityList;
    
    public bool TryGetEffectColor(CrowdControlType type, out Color color)
    {
        if (_effectColorDictionary == null)
        {
            color = Color.white;
            return false;
        }
        return _effectColorDictionary.TryGetValue(type, out color);
    }
    
    // ---------- 보조 함수 ----------
    private Dictionary<CrowdControlType, Color> BuildDictionary()
    {
        var dict = new Dictionary<CrowdControlType, Color>();

        if (_effectColorMappings == null) return dict;

        foreach (var mapping in _effectColorMappings)
        {
            dict.TryAdd(mapping.CrowdControlType, mapping.Color);
        }
        
        return dict;
    }
    
    private void ValidateData()
    {
        if (_ccPriorityList != null && _ccPriorityList.Count > 0)
        {
            var tempPrioritySet = new HashSet<CrowdControlType>();
            for (int i = 0; i < _ccPriorityList.Count; i++)
            {
                if (!tempPrioritySet.Add(_ccPriorityList[i]))
                {
                    Debug.LogWarning($"[StateFeedBackColorSO] CC 우선순위 리스트에 중복 발생: {_ccPriorityList[i]} (인덱스 {i}). 의도치 않은 동작이 발생할 수 있습니다.");
                }
            }
        }
        
        if (_effectColorMappings != null && _effectColorMappings.Count > 0)
        {
            var tempColorSet = new HashSet<CrowdControlType>();
            for (int i = 0; i < _effectColorMappings.Count; i++)
            {
                if (!tempColorSet.Add(_effectColorMappings[i].CrowdControlType))
                {
                    Debug.LogWarning($"[StateFeedBackColorSO] 컬러 맵핑 중복 발생: {_effectColorMappings[i].CrowdControlType} (인덱스 {i}). 앞선 인덱스의 색상이 강제 유지됩니다.");
                }
            }
        }
    }
}

[Serializable]
public class EffectColorMapping
{
    public CrowdControlType CrowdControlType;
    public Color Color;
}