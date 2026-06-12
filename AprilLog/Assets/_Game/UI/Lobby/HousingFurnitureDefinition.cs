//담당자: 조규민
//설명: 하우징 가구의 고정 데이터와 프로토타입 표시 정보를 정의한다.

using System;
using UnityEngine;

/// <summary>
/// 하우징 슬롯에 착용할 수 있는 가구의 고정 정보를 담는다.
/// </summary>
[Serializable]
public class HousingFurnitureDefinition
{
    [SerializeField] private int _furnitureId;
    [SerializeField] private int _slotId;
    [SerializeField] private string _displayName;
    [SerializeField] private HousingFurnitureType _furnitureType;
    [SerializeField] private HousingFurnitureCategory _category;
    [SerializeField] private HousingLayerType _layerType;
    [SerializeField] private HousingUiFunctionType _uiFunctionType;
    [SerializeField] private int _unlockChapter;
    [SerializeField] private int _goldPrice;
    [SerializeField] private int _parchmentPrice;
    [SerializeField] private bool _isDefaultOwned;
    [SerializeField] private string _interactionMessage;
    [SerializeField] private Color _prototypeColor;

    public int FurnitureId => _furnitureId;
    public int SlotId => _slotId;
    public string DisplayName => _displayName;
    public HousingFurnitureType FurnitureType => _furnitureType;
    public HousingFurnitureCategory Category => _category;
    public HousingLayerType LayerType => _layerType;
    public HousingUiFunctionType UiFunctionType => _uiFunctionType;
    public int UnlockChapter => _unlockChapter;
    public int GoldPrice => _goldPrice;
    public int ParchmentPrice => _parchmentPrice;
    public bool IsDefaultOwned => _isDefaultOwned;
    public string InteractionMessage => _interactionMessage;
    public Color PrototypeColor => _prototypeColor;

    public HousingFurnitureDefinition(
        int _furnitureId,
        int _slotId,
        string _displayName,
        HousingFurnitureType _furnitureType,
        HousingFurnitureCategory _category,
        HousingLayerType _layerType,
        HousingUiFunctionType _uiFunctionType,
        int _unlockChapter,
        int _goldPrice,
        int _parchmentPrice,
        bool _isDefaultOwned,
        string _interactionMessage,
        Color _prototypeColor)
    {
        // 기능: 가구 고정 데이터를 생성하고 해금 챕터와 가격 값은 유효 범위로 보정한다.
        this._furnitureId = _furnitureId;
        this._slotId = _slotId;
        this._displayName = _displayName;
        this._furnitureType = _furnitureType;
        this._category = _category;
        this._layerType = _layerType;
        this._uiFunctionType = _uiFunctionType;
        this._unlockChapter = Mathf.Max(1, _unlockChapter);
        this._goldPrice = Mathf.Max(0, _goldPrice);
        this._parchmentPrice = Mathf.Max(0, _parchmentPrice);
        this._isDefaultOwned = _isDefaultOwned;
        this._interactionMessage = _interactionMessage;
        this._prototypeColor = _prototypeColor;
    }
}
