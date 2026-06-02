using UnityEngine;

public class AspectRatioHandler : MonoBehaviour
{
    [SerializeField] private GameObject _phoneLayout;
    [SerializeField] private GameObject _tabletLayout;

    void Start()
    {
        float aspectRatio = (float)Screen.width / Screen.height;

        if (aspectRatio >= 0.75f)
        {
            _phoneLayout.SetActive(false);
            _tabletLayout.SetActive(true);
        }
        else
        {
            _phoneLayout.SetActive(true);
            _tabletLayout.SetActive(false);
        }
    }
}