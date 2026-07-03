using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class UnitVisualController : MonoBehaviour
{
    [SerializeField] private Image _unitImage;
    [SerializeField] private GameObject[] _enchantObjects;

    private void OnEnable()
    {
        if (_unitImage != null)
            _unitImage.color = Color.white;
    }

    public void PlayJokerEffect(Action onComplete)
    {
        _unitImage.DOKill();

        _unitImage.DOColor(Color.gray, 1.0f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                _unitImage.color = Color.white;
                onComplete?.Invoke();
            });
    }

    public void PlayEnchantEffect(int count, Action onComplete)
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i <= count && i < _enchantObjects.Length; i++)
        {
            int index = i;

            seq.AppendCallback(() => ToggleObjects(index));
            
            seq.AppendInterval(1.0f);
 
        }
        seq.OnComplete(() => onComplete?.Invoke());
    }

    private void ToggleObjects(int activeIndex)
    {
        for (int i = 0; i < _enchantObjects.Length; i++)
            _enchantObjects[i].SetActive(i == activeIndex);
    }

    public void ResetToDefault() => ToggleObjects(0);
}
