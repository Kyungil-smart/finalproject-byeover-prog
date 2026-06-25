using UnityEngine;
using UnityEngine.UI;

public class JokerData : MonoBehaviour
{
    public Image iconImage;

    public void ActivateJokerImage()
    {
        if (iconImage != null)
        {
            iconImage.enabled = true;
            Debug.Log($"{gameObject.name} 조커 이미지 활성화!");
        }
    }
}
