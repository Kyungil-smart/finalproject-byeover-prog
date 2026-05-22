// 담당자 : 정승우
// 설명   : 재화 Model -- 골드, 양피지

using System;
using UnityEngine;

/// <summary>
/// 골드와 양피지 보유량을 관리한다. 값 바뀌면 이벤트 발행.
/// </summary>
public class CurrencyModel : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int, int> OnCurrencyChanged;    // gold, parchment

    // ---------- 데이터 ----------
    public int Gold { get; private set; }
    public int Parchment { get; private set; }

    // ---------- 초기화 ----------
    public void Initialize(int gold, int parchment)
    {
        Gold = gold;
        Parchment = parchment;
        OnCurrencyChanged?.Invoke(Gold, Parchment);
    }

    // ---------- 조작 ----------
    public void AddGold(int amount)
    {
        Gold += amount;
        OnCurrencyChanged?.Invoke(Gold, Parchment);
    }

    public void AddParchment(int amount)
    {
        Parchment += amount;
        OnCurrencyChanged?.Invoke(Gold, Parchment);
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        OnCurrencyChanged?.Invoke(Gold, Parchment);
        return true;
    }

    public bool SpendParchment(int amount)
    {
        if (Parchment < amount) return false;
        Parchment -= amount;
        OnCurrencyChanged?.Invoke(Gold, Parchment);
        return true;
    }

    public bool CanAfford(int gold, int parchment)
    {
        return Gold >= gold && Parchment >= parchment;
    }
}