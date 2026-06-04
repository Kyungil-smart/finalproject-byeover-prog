// 담당자 : 정승우
// 설명   : 런타임 사각형 스프라이트 생성 유틸 (플레이스홀더 비주얼/투사체 공용)

using UnityEngine;

/// <summary>
/// 에셋 없이 코드로 사각형 스프라이트를 만든다. 색은 SpriteRenderer.color로 입힌다.
/// </summary>
public static class SpriteFactory
{
    private static Sprite _square;

    /// <summary>1x1 단위 흰색 사각형 스프라이트(공유 캐시).</summary>
    public static Sprite Square()
    {
        if (_square != null) return _square;

        var tex = new Texture2D(2, 2);
        var pixels = new Color[4];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        _square = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        _square.name = "RuntimeSquare";
        return _square;
    }
}
