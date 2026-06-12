//담당자: 조규민
//설명: 하우징 가구의 상호작용 방식, 배치 분류, 기능형 연결 대상을 정의한다.

/// <summary>
/// 하우징 가구가 플레이어와 캐릭터에 반응하는 방식을 구분한다.
/// </summary>
public enum HousingFurnitureType
{
    None,
    Decoration,
    Interaction,
    UiFunction,
    Background
}

/// <summary>
/// 데코 팝업과 배치 레이어에서 사용하는 가구 분류를 나타낸다.
/// </summary>
public enum HousingFurnitureCategory
{
    None,
    Background,
    Large,
    Medium,
    Small
}

/// <summary>
/// 하우징 화면에서 뒤에서 앞으로 표시되는 배치 레이어를 나타낸다.
/// </summary>
public enum HousingLayerType
{
    Background = 0,
    LargeFurniture = 1,
    MediumFurniture = 2,
    SmallFurniture = 3,
    Character = 4
}

/// <summary>
/// 프로토타입 하우징에서 고정 배치로 사용하는 슬롯 ID를 나타낸다.
/// </summary>
public static class HousingSlotId
{
    public const int FullBackground = 0;
    public const int Wallpaper = 1;
    public const int Floor = 2;
    public const int Bed = 10;
    public const int Bookcase = 11;
    public const int DiningTable = 20;
    public const int Plant = 30;
    public const int Character = 100;
}

/// <summary>
/// UI 기능형 가구가 연결할 하우징 기능을 나타낸다.
/// </summary>
public enum HousingUiFunctionType
{
    None,
    StoryReplay,
    CoffeeMachine,
    ProfileArchive,
    GoldGenerator,
    Closet
}
