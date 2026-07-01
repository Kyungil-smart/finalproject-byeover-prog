using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class UnitVisualController : MonoBehaviour
{
    [SerializeField] private Image _unitImage;

    private void OnEnable()
    {
        if (_unitImage != null)
            _unitImage.color = Color.white;
    }

    public void PlayJokerEffect(Action onComplete)
    {
        _unitImage.DOKill();
        _unitImage.color = Color.white;

        _unitImage.DOColor(Color.gray, 1.0f)
            .SetUpdate(true)
            .OnComplete(() => {
                gameObject.SetActive(false);
                _unitImage.color = Color.white;
                onComplete?.Invoke();
            });
    }

    public void PlayEnchantEffect(int count)
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        for (int i = 0; i < count; i++)
        {
            seq.Append(_unitImage.DOColor(Color.yellow, 0.2f).SetUpdate(true));
            seq.Append(_unitImage.DOColor(Color.white, 0.2f).SetUpdate(true));
        }
    }
}
