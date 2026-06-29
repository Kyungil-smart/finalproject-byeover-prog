using DG.Tweening;
using UnityEngine;
using System;

public class EliteRewardEffect : MonoBehaviour
{
    [SerializeField] private RectTransform _EffectImageRect;

    public void PlayRewardEffect(Action onComplete)
    {
        _EffectImageRect.gameObject.SetActive(true);
        _EffectImageRect.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();

        seq.Append(_EffectImageRect.DOScale(1.2f, 1.0f).SetEase(Ease.OutBack));

        seq.Append(_EffectImageRect.DOScale(1.0f, 0.7f)).SetEase(Ease.OutCubic);

        seq.OnComplete(() => {
            _EffectImageRect.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }
}
