// 담당자 : 조규민
// Safe Area 보정을 적용할 화면 가장자리 조합 정의
// 설명   : Safe Area 적용 방향 플래그

// 수정자 : 정승우
// 수정내용 : Safe Area API 분리를 위한 적용 방향 정의 추가

using System;

[Flags]
public enum SafeAreaEdges
{
    // Safe Area를 적용하지 않는다.
    None = 0,
    // 각 화면 가장자리별 Safe Area 적용 여부다.
    Left = 1 << 0,
    Right = 1 << 1,
    Top = 1 << 2,
    Bottom = 1 << 3,
    // 가로 또는 세로 방향 가장자리를 한 번에 적용한다.
    Horizontal = Left | Right,
    Vertical = Top | Bottom,
    // 모든 가장자리에 Safe Area를 적용한다.
    All = Horizontal | Vertical
}
