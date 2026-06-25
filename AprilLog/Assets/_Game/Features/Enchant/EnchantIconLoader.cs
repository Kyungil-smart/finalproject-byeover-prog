//담당자: 조규민

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인챈트 아이콘 키를 UI Image에 적용한다.
/// </summary>
public static class EnchantIconLoader
{
    private const string _iconResourceFolder = "EnchantIcons/";

    public static void ApplyIcon(Image targetImage, string imageKey)
    {
        if (targetImage == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(imageKey) || imageKey == "0")
        {
            ClearIcon(targetImage);
            return;
        }

        Sprite icon = Resources.Load<Sprite>(_iconResourceFolder + imageKey);
        if (icon == null)
        {
            ClearIcon(targetImage);
            Debug.LogWarning($"[EnchantIconLoader] 인챈트 아이콘을 찾을 수 없습니다. 경로: Resources/{_iconResourceFolder}{imageKey}");
            return;
        }

        targetImage.sprite = icon;
        targetImage.enabled = true;
        targetImage.preserveAspect = true;
    }

    private static void ClearIcon(Image targetImage)
    {
        targetImage.sprite = null;
        targetImage.enabled = false;
    }
}
