//담당자: 조규민

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 배치 하단 팝업을 표시합니다.
/// </summary>
public class HousingPlacementPopupView : MonoBehaviour
{
    [Serializable]
    public class CategoryButtonBinding
    {
        [Header("카테고리")]
        [SerializeField] private HousingPlacementCategory _category;
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _labelText;

        public HousingPlacementCategory Category => _category;
        public Button Button => _button;
        public TextMeshProUGUI LabelText => _labelText;
    }

    [Header("카테고리 버튼")]
    [SerializeField] private CategoryButtonBinding[] _categoryButtons;

    [Header("아이템 목록")]
    [SerializeField] private Transform _itemContent;
    [SerializeField] private HousingPlacementItemSlotView _itemSlotPrefab;
    [SerializeField] private TextMeshProUGUI _emptyText;

    [Header("선택 표시")]
    [Tooltip("켜면 선택된 카테고리 텍스트 색상을 View에서 변경합니다. 직접 지정한 버튼 색상을 유지하려면 끕니다.")]
    [SerializeField] private bool _useCategorySelectionTextColor;

    private readonly List<HousingPlacementItemSlotView> _spawnedSlots = new();

    public event Action<HousingPlacementCategory> OnCategoryClicked;
    public event Action<HousingPlacementItemData> OnItemClicked;

    private void Awake()
    {
        BindCategories();
    }

    private void OnDestroy()
    {
        UnbindCategories();
        ClearItems();
    }

    public void RefreshItems(IReadOnlyList<HousingPlacementItemData> _items, HousingPlacementCategory _selectedCategory)
    {
        ClearItems();
        RefreshCategoryLabels(_selectedCategory);

        int _itemCount = _items == null ? 0 : _items.Count;

        if (_emptyText != null)
        {
            _emptyText.gameObject.SetActive(_itemCount <= 0);
        }

        if (_itemContent == null || _itemSlotPrefab == null)
        {
            return;
        }

        for (int _index = 0; _index < _itemCount; _index++)
        {
            HousingPlacementItemSlotView _slot = Instantiate(_itemSlotPrefab, _itemContent);
            _slot.SetData(_items[_index]);
            _slot.OnClicked += HandleItemClicked;
            _spawnedSlots.Add(_slot);
        }
    }

    private void BindCategories()
    {
        if (_categoryButtons == null)
        {
            return;
        }

        for (int _index = 0; _index < _categoryButtons.Length; _index++)
        {
            CategoryButtonBinding _binding = _categoryButtons[_index];

            if (_binding?.Button == null)
            {
                continue;
            }

            HousingPlacementCategory _category = _binding.Category;
            _binding.Button.transition = Selectable.Transition.None;
            _binding.Button.onClick.RemoveAllListeners();
            _binding.Button.onClick.AddListener(() => OnCategoryClicked?.Invoke(_category));
        }
    }

    private void UnbindCategories()
    {
        if (_categoryButtons == null)
        {
            return;
        }

        for (int _index = 0; _index < _categoryButtons.Length; _index++)
        {
            CategoryButtonBinding _binding = _categoryButtons[_index];

            if (_binding?.Button == null)
            {
                continue;
            }

            _binding.Button.onClick.RemoveAllListeners();
        }
    }

    private void RefreshCategoryLabels(HousingPlacementCategory _selectedCategory)
    {
        if (_useCategorySelectionTextColor == false)
        {
            return;
        }

        if (_categoryButtons == null)
        {
            return;
        }

        for (int _index = 0; _index < _categoryButtons.Length; _index++)
        {
            CategoryButtonBinding _binding = _categoryButtons[_index];

            if (_binding?.LabelText == null)
            {
                continue;
            }

            bool _isSelected = _binding.Category == _selectedCategory;
            _binding.LabelText.color = _isSelected ? Color.white : new Color(0.72f, 0.72f, 0.72f);
        }
    }

    private void ClearItems()
    {
        for (int _index = 0; _index < _spawnedSlots.Count; _index++)
        {
            HousingPlacementItemSlotView _slot = _spawnedSlots[_index];

            if (_slot == null)
            {
                continue;
            }

            _slot.OnClicked -= HandleItemClicked;
            Destroy(_slot.gameObject);
        }

        _spawnedSlots.Clear();
    }

    private void HandleItemClicked(HousingPlacementItemData _itemData)
    {
        OnItemClicked?.Invoke(_itemData);
    }
}
