// 담당자 : 김영찬
// 설명   : 스토리/시나리오 대사 및 캐릭터 데이터 저장소

using System;
using System.Collections.Generic;
using UnityEngine;

public class StoryRepo : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("스토리 테이블 에셋")]
    [SerializeField] private Story_TalkTable _talkTable; 
    [SerializeField] private Story_CharacterTable _characterTable;
    [SerializeField] private StoryTriggerTable _triggerTable;

    // ---------- Dictionary ----------
    private Dictionary<int, Story_CharacterData> _character; // ID, data
    private Dictionary<int, List<Story_TalkData>> _talkGroup; // GroupID, 그룹 내 텍스트 데이터의 List
    private Dictionary<(int, string), StoryTriggerData> _triggersByChapterID; // (ChapterID, TriggerType), data
    private Dictionary<int, StoryTriggerData>  _triggersByTriggerId;

    private bool _isInitialized = false;

    // ---------- Initialize ----------
    public void Initialize()
    {
        if (_isInitialized) return;

        _character = BuildDictionary(_characterTable, nameof(_characterTable), r => r.ID);
        _talkGroup = BuildTalkGroup();
        _triggersByChapterID = BuildDictionary(_triggerTable, nameof(_triggerTable), r => (r.Target_ID, r.TriggerType));
        _triggersByTriggerId = BuildDictionary(_triggerTable, nameof(_triggerTable), r => r.StoryTrigger_ID);

        _isInitialized = true;
        Debug.Log($"[StoryRepo] Initialized! Talk Groups: {_talkGroup.Count}, Characters: {_character.Count}");
    }

    // ---------- 조회 API ----------
    public Story_CharacterData GetCharacterData(int charId) => GetData(_character, charId, nameof(GetCharacterData));
    public List<Story_TalkData> GetTalkGroup(int groupId) => GetData(_talkGroup, groupId, nameof(GetTalkGroup));
    public StoryTriggerData GetTriggerDataByChapterID(int chapterID, string triggerType) => GetData(_triggersByChapterID, (chapterID, triggerType), nameof(GetTriggerDataByChapterID));
    public StoryTriggerData GetTriggerDataByTriggerID(int triggerId) => GetData(_triggersByTriggerId, triggerId, nameof(GetTriggerDataByTriggerID));
    
    // ---------- 보조 함수 ----------
    private Dictionary<TKey, TData> BuildDictionary<TData, TKey>(
        DataTable<TData> table,
        string tableName,
        Func<TData, TKey> keySelector)
        where TData : class
    {
        var result = new Dictionary<TKey, TData>();

        if (table == null)
        {
            Debug.LogWarning($"[StoryRepo] {tableName} is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[StoryRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[StoryRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[StoryRepo] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }
    
    private TData GetData<TKey, TData>(Dictionary<TKey, TData> dictionary, TKey key, string methodName)
        where TData : class
    {
        if (dictionary == null)
        {
            Debug.LogWarning($"[StoryRepo] {methodName} cache is not initialized.");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[StoryRepo] {methodName} data not found. Key: {key}");
        return null;
    }

    private Dictionary<int, List<Story_TalkData>> BuildTalkGroup()
    {
        var result = new Dictionary<int, List<Story_TalkData>>();
        
        if (_talkTable == null)
        {
            Debug.LogWarning("[StoryRepo] Story_TalkTable is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (_talkTable.rows == null)
        {
            Debug.LogWarning("[StoryRepo] Story_TalkTable.rows is null. Empty dictionary will be used.");
            return result;
        }

        foreach (var talkData in _talkTable.rows)
        {
            // 해당 GroupID의 리스트가 아직 없다면 새로 생성
            if (!result.ContainsKey(talkData.GroupID))
            {
                result[talkData.GroupID] = new List<Story_TalkData>();
            }

            // 그룹 리스트에 대사 추가
            result[talkData.GroupID].Add(talkData);
        }

        // 각 그룹 내의 대사들이 DB 순서와 다르게 섞였을 경우를 대비하여 ID 순으로 정렬
        foreach (var groupList in result.Values)
        {
            groupList.Sort((a, b) => a.ID.CompareTo(b.ID));
        }
        
        return result;
    }
}