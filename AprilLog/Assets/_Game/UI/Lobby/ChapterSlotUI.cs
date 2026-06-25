// 담당자 : 홍정옥
// 설명   : 챕터 슬롯 카드 — ChapterTestEntry 데이터를 UI에 표시 (Page_MainLobby용)

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterSlotUI : MonoBehaviour
{
    [Header("챕터 정보")]
    [Tooltip("챕터 이름 텍스트 (예: 어둠의 숲)")]
    [SerializeField] private TMP_Text _chapterNameText;

    [Tooltip("챕터 표시 텍스트 (예: CHAPTER.1)")]
    [SerializeField] private TMP_Text _chapterLabelText;

    [Tooltip("챕터 설명 텍스트")]
    [SerializeField] private TMP_Text _descriptionText;

    [Tooltip("챕터 대표 이미지")]
    [SerializeField] private Image    _chapterImage;

    [Tooltip("이미지가 없을 때 표시할 기본 스프라이트 (선택)")]
    [SerializeField] private Sprite   _placeholderSprite;

    // ------------------------------------------------------------------
    public void SetData(ChapterTestEntry entry, int chapterIndex)
    {
        if (entry == null) return;

        string name  = string.IsNullOrWhiteSpace(entry.chapterName)
            ? $"Chapter {chapterIndex + 1}"
            : entry.chapterName;

        string label = string.IsNullOrWhiteSpace(entry.chapterLabel)
            ? $"CHAPTER.{chapterIndex + 1}"
            : entry.chapterLabel;

        SetText(_chapterNameText,  name);
        SetText(_chapterLabelText, label);
        SetText(_descriptionText,  entry.description);

        if (_chapterImage != null)
            _chapterImage.sprite = entry.image != null ? entry.image : _placeholderSprite;
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
            label.text = value ?? string.Empty;
    }
}
