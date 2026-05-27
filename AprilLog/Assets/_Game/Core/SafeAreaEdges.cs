// 담당자 : 조규민
// 설명   : Safe Area 적용 방향 플래그
// 수정자 : Codex
// 수정내용 : Safe Area API 분리를 위한 적용 방향 정의 추가

using System;

[Flags]
public enum SafeAreaEdges
{
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Top = 1 << 2,
    Bottom = 1 << 3,
    Horizontal = Left | Right,
    Vertical = Top | Bottom,
    All = Horizontal | Vertical
}
