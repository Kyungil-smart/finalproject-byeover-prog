//담당자: 조규민
// 퍼즐 책 슬롯 가장자리에 내부 테두리를 생성하고 슬롯 색상 변화에 맞춰 갱신

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퍼즐 테이블 책 슬롯 안쪽에 흰색 테두리를 표시한다.
/// </summary>
public class PuzzleSlotInnerBorderView : MonoBehaviour
{
    [Header("테두리")]
    [SerializeField] private Color _borderColor = Color.white;

    [Tooltip("책 이미지 안쪽으로 표시할 테두리 두께입니다.")]
    [SerializeField] private float _borderThickness = 3f;

    private readonly List<SlotBorder> _slotBorders = new List<SlotBorder>();

    private void Awake()
    {
        BuildBorders();
        RefreshBorders();
    }

    private void LateUpdate()
    {
        RefreshBorders();
    }

    // 퍼즐 책 슬롯만 선별해 네 방향 내부 테두리 생성
    private void BuildBorders()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (!IsPuzzleBookSlot(image))
            {
                continue;
            }

            _slotBorders.Add(CreateSlotBorder(image));
        }
    }

    private bool IsPuzzleBookSlot(Image image)
    {
        if (image == null || image.transform.parent == null)
        {
            return false;
        }

        string parentName = image.transform.parent.name;
        return parentName.StartsWith("Table (") || parentName.StartsWith("WtTable (");
    }

    private SlotBorder CreateSlotBorder(Image slotImage)
    {
        RectTransform parent = slotImage.rectTransform;
        var root = new GameObject("InnerWhiteBorder", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        root.transform.SetAsLastSibling();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image top = CreateLine(rootRect, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f));
        Image bottom = CreateLine(rootRect, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f));
        Image left = CreateLine(rootRect, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f));
        Image right = CreateLine(rootRect, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f));

        ApplyHorizontalLine(top.rectTransform, true);
        ApplyHorizontalLine(bottom.rectTransform, false);
        ApplyVerticalLine(left.rectTransform, true);
        ApplyVerticalLine(right.rectTransform, false);

        return new SlotBorder(slotImage, root, top, bottom, left, right);
    }

    private Image CreateLine(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var line = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        line.transform.SetParent(parent, false);

        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;

        Image image = line.GetComponent<Image>();
        image.color = _borderColor;
        image.raycastTarget = false;
        return image;
    }

    private void ApplyHorizontalLine(RectTransform rect, bool isTop)
    {
        rect.offsetMin = new Vector2(0f, isTop ? -_borderThickness : 0f);
        rect.offsetMax = new Vector2(0f, isTop ? 0f : _borderThickness);
    }

    private void ApplyVerticalLine(RectTransform rect, bool isLeft)
    {
        rect.offsetMin = new Vector2(isLeft ? 0f : -_borderThickness, _borderThickness);
        rect.offsetMax = new Vector2(isLeft ? _borderThickness : 0f, -_borderThickness);
    }

    // 원본 슬롯 색상을 각 테두리 색상에 동기화
    private void RefreshBorders()
    {
        foreach (SlotBorder slotBorder in _slotBorders)
        {
            slotBorder.Refresh(_borderColor);
        }
    }

    private sealed class SlotBorder
    {
        private readonly Image _slotImage;
        private readonly GameObject _root;
        private readonly Image[] _lines;

        public SlotBorder(Image slotImage, GameObject root, params Image[] lines)
        {
            _slotImage = slotImage;
            _root = root;
            _lines = lines;
        }

        public void Refresh(Color color)
        {
            bool isVisible = _slotImage != null && _slotImage.enabled && _slotImage.sprite != null;
            if (_root.activeSelf != isVisible)
            {
                _root.SetActive(isVisible);
            }

            foreach (Image line in _lines)
            {
                if (line != null)
                {
                    line.color = color;
                }
            }
        }
    }
}
