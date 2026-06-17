using UnityEngine;

public class AspectRatioHandler : MonoBehaviour
{
    [SerializeField] private GameObject _phoneLayout;
    [SerializeField] private GameObject _tabletLayout;

    void Start()
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        bool tablet = aspectRatio >= 0.75f;

        // 씬 미배선 방어: 레이아웃이 인스펙터에 안 꽂혀 있으면 SetActive에서 예외 → null 가드.
        if (_phoneLayout != null) _phoneLayout.SetActive(!tablet);
        if (_tabletLayout != null) _tabletLayout.SetActive(tablet);
    }
}