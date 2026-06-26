public static class PixelConverter
{
    /// <summary>
    /// 프로젝트의 기본 PPU (Pixels Per Unit)<br/>
    /// ToDo : 기본 PPU가 변화 하게 되면 해당 변수를 수정 해야됨
    /// </summary>
    public const float DEFAULT_PPU = 100f;
    
    /// <summary>
    /// 픽셀 단위 속도(Pixel/sec)를 유니티 단위 속도(Unit/sec)로 변환
    /// </summary>
    public static float PixelSpeedToUnitySpeed(float pixelSpeed, float ppu = DEFAULT_PPU)
    {
        return pixelSpeed / ppu;
    }
    
    /// <summary>
    /// 유니티 단위 속도(Unit/sec)를 다시 픽셀 단위 속도(Pixel/sec)로 역변환 (디버깅/기획자 전달용)
    /// </summary>
    public static float UnitySpeedToPixelSpeed(float unitSpeed, float ppu = DEFAULT_PPU)
    {
        return unitSpeed * ppu;
    }
    
    /// <summary>
    /// 픽셀(거리)를 유닛(유니티 기본 단위에 따른 거리)로 변환
    /// </summary>
    public static float PixelsToUnits(float pixels, float ppu = DEFAULT_PPU)
    {
        return pixels / ppu;
    }
    
    /// <summary>
    /// 유닛(유니티 기본 단위에 따른 거리)을 픽셀(거리)로 역변환 (디버깅/기획자 전달용)
    /// </summary>
    public static float UnitsToPixels(float units, float ppu = DEFAULT_PPU)
    {
        return units * ppu;
    }
}
