//담당자: 조규민

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조합식 재료칸이 비어 있을 때 기본 흰색 이미지를 숨긴다.
/// </summary>
public class CombinationEmptySlotAlphaView : MonoBehaviour
{
    [Header("투명도")]
    [SerializeField] private float _emptyAlpha = 0f;
    [SerializeField] private float _filledAlpha = 1f;

    private Image[] _ingredientImages;

    private void Awake()
    {
        CacheIngredientImages();
        RefreshAlpha();
    }

    private void LateUpdate()
    {
        RefreshAlpha();
    }

    private void CacheIngredientImages()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        int count = 0;
        foreach (Image image in images)
        {
            if (IsIngredientImage(image))
            {
                count++;
            }
        }

        _ingredientImages = new Image[count];
        int index = 0;
        foreach (Image image in images)
        {
            if (IsIngredientImage(image))
            {
                _ingredientImages[index] = image;
                index++;
            }
        }
    }

    private bool IsIngredientImage(Image image)
    {
        if (image == null)
        {
            return false;
        }

        string name = image.gameObject.name;
        return name == "Unit_1" || name == "Unit_2" || name == "Unit_3";
    }

    private void RefreshAlpha()
    {
        if (_ingredientImages == null)
        {
            return;
        }

        foreach (Image image in _ingredientImages)
        {
            if (image == null)
            {
                continue;
            }

            Color color = image.color;
            color.a = image.sprite == null ? _emptyAlpha : _filledAlpha;
            image.color = color;
        }
    }
}
