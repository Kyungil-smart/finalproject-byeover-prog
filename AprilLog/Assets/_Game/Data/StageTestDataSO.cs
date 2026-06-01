// 담당자 : 홍정옥
// 설명   : 기획자 테스트용 챕터 데이터 SO (Page_MainLobby용)
//          - 나중에 실제 StageDataTable 연동으로 교체 예정
//
// UI 표시 구조
//   chapterName       ← 챕터 이름   (예: "어둠의 숲")
//   chapterLabel      ← 챕터 표시   (예: "CHAPTER.1")
//   description       ← 챕터 설명   (예: "빛이 닿지 않는 깊은 숲...")
//   image             ← 챕터 이미지

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChapterTestEntry
{
    [Tooltip("챕터 이름 (예: 어둠의 숲)")]
    public string chapterName;

    [Tooltip("챕터 표시 텍스트 (예: CHAPTER.1) — 비워두면 자동 생성")]
    public string chapterLabel;

    [TextArea(2, 4)]
    [Tooltip("챕터 설명")]
    public string description;

    [Tooltip("챕터 대표 이미지")]
    public Sprite image;
}

[CreateAssetMenu(fileName = "ChapterTestData", menuName = "Test/Chapter Test Data")]
public class ChapterTestDataSO : ScriptableObject
{
    [Header("챕터 목록 (순서대로)")]
    public List<ChapterTestEntry> chapters = new();

    public int ChapterCount => chapters != null ? chapters.Count : 0;

    /// <summary>인덱스(0-based)로 챕터 데이터를 반환합니다.</summary>
    public ChapterTestEntry GetChapter(int index)
    {
        if (chapters == null || chapters.Count == 0) return null;

        index = Mathf.Clamp(index, 0, chapters.Count - 1);
        var entry = chapters[index];

        // label 비어있으면 자동 생성
        if (string.IsNullOrWhiteSpace(entry.chapterLabel))
            entry.chapterLabel = $"CHAPTER.{index + 1}";

        // 이름 비어있으면 자동 생성
        if (string.IsNullOrWhiteSpace(entry.chapterName))
            entry.chapterName = $"Chapter {index + 1}";

        return entry;
    }

#if UNITY_EDITOR
    [ContextMenu("10챕터 자동 채우기")]
    private void FillTenChapters()
    {
        chapters = new List<ChapterTestEntry>();
        for (int i = 1; i <= 10; i++)
        {
            chapters.Add(new ChapterTestEntry
            {
                chapterName  = $"챕터 이름 {i}",
                chapterLabel = $"CHAPTER.{i}",
                description  = $"{i}번째 챕터 설명입니다. 기획자가 내용을 채워주세요.",
                image        = null,
            });
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[ChapterTestDataSO] 10챕터 자동 채우기 완료");
    }
#endif
}
