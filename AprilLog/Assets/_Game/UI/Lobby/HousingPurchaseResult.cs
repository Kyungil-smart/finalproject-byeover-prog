//담당자: 조규민
//설명: 하우징 가구 구매와 착용 요청의 처리 결과를 정의한다.

/// <summary>
/// 하우징 가구 구매/착용 요청의 성공 여부와 실패 원인을 나타낸다.
/// </summary>
public enum HousingPurchaseResult
{
    Success,
    InvalidFurniture,
    Locked,
    AlreadyOwned,
    NotOwned,
    AlreadyEquipped,
    SlotMismatch,
    NotEnoughCurrency
}
