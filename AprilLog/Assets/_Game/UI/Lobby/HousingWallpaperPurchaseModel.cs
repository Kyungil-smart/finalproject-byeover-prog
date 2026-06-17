//담당자: 조규민
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하우징 벽지 추가 슬롯의 구매 비용과 현재 세션 구매 상태를 담당한다.
/// </summary>
public class HousingWallpaperPurchaseModel : MonoBehaviour
{
    private const int GoldSlotStartIndex = 12;
    private const int GoldSlotEndIndex = 15;
    private const int ParchmentSlotStartIndex = 16;
    private const int ParchmentSlotEndIndex = 19;

    [Header("골드 구매 비용")]
    [Tooltip("Slot_Wallpaper_13~16 순서로 적용됩니다.")]
    [SerializeField] private int[] _goldPrices = { 500, 700, 900, 1200 };

    [Header("양피지 구매 비용")]
    [Tooltip("Slot_Wallpaper_17~20 순서로 적용됩니다.")]
    [SerializeField] private int[] _parchmentPrices = { 5, 8, 12, 16 };

    public event Action PurchaseStateChanged;

    private readonly HashSet<int> _purchasedSlotIndexes = new HashSet<int>();
    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_isInitialized)
            return;

        _purchasedSlotIndexes.Clear();
        _isInitialized = true;
    }

    public bool IsPurchaseSlot(int _slotIndex)
    {
        return IsGoldSlot(_slotIndex) || IsParchmentSlot(_slotIndex);
    }

    public bool IsPurchased(int _slotIndex)
    {
        if (!IsPurchaseSlot(_slotIndex))
            return true;

        Initialize();
        return _purchasedSlotIndexes.Contains(_slotIndex);
    }

    public bool CanPurchase(int _slotIndex, CurrencyModel _currencyModel)
    {
        if (_currencyModel == null)
            return false;

        if (!IsPurchaseSlot(_slotIndex))
            return false;

        if (IsPurchased(_slotIndex))
            return false;

        return _currencyModel.CanAfford(GetGoldPrice(_slotIndex), GetParchmentPrice(_slotIndex));
    }

    public bool TryPurchase(int _slotIndex, CurrencyModel _currencyModel)
    {
        if (IsPurchased(_slotIndex))
            return true;

        if (!CanPurchase(_slotIndex, _currencyModel))
            return false;

        if (!_currencyModel.TrySpend(GetGoldPrice(_slotIndex), GetParchmentPrice(_slotIndex)))
            return false;

        MarkPurchased(_slotIndex);
        return true;
    }

    public string GetPriceText(int _slotIndex)
    {
        if (IsGoldSlot(_slotIndex))
            return "Gold " + CurrencyModel.FormatAmount(GetGoldPrice(_slotIndex));

        if (IsParchmentSlot(_slotIndex))
            return "Parchment " + CurrencyModel.FormatAmount(GetParchmentPrice(_slotIndex));

        return string.Empty;
    }

    private bool IsGoldSlot(int _slotIndex)
    {
        return _slotIndex >= GoldSlotStartIndex && _slotIndex <= GoldSlotEndIndex;
    }

    private bool IsParchmentSlot(int _slotIndex)
    {
        return _slotIndex >= ParchmentSlotStartIndex && _slotIndex <= ParchmentSlotEndIndex;
    }

    private int GetGoldPrice(int _slotIndex)
    {
        if (!IsGoldSlot(_slotIndex))
            return 0;

        return GetPrice(_goldPrices, _slotIndex - GoldSlotStartIndex);
    }

    private int GetParchmentPrice(int _slotIndex)
    {
        if (!IsParchmentSlot(_slotIndex))
            return 0;

        return GetPrice(_parchmentPrices, _slotIndex - ParchmentSlotStartIndex);
    }

    private int GetPrice(int[] _prices, int _priceIndex)
    {
        if (_prices == null || _prices.Length == 0)
            return 0;

        int _safeIndex = Mathf.Clamp(_priceIndex, 0, _prices.Length - 1);
        return Mathf.Max(0, _prices[_safeIndex]);
    }

    private void MarkPurchased(int _slotIndex)
    {
        Initialize();

        if (!_purchasedSlotIndexes.Add(_slotIndex))
            return;

        PurchaseStateChanged?.Invoke();
    }
}
