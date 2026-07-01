using DG.Tweening;
using UnityEngine;
using System;

public class EliteRewardEffect : MonoBehaviour
{
    [SerializeField] private GameObject _container;
    [SerializeField] private RectTransform _EffectImageRect;

    public UnitVisualController EnchantUnit;
    public UnitVisualController joker1;
    public UnitVisualController joker2;

    private int _enchantCount;
      
    public void SetEnchantCount(int count)
    {
        _enchantCount = count;
    }

    public void PlayRewardEffect(Action onComplete)
    {
        _container.SetActive(true);

        /*joker1.gameObject.SetActive(false);
        joker2.gameObject.SetActive(false);
        EnchantUnit.gameObject.SetActive(false);*/

        /*joker1.gameObject.SetActive(true);
        joker2.gameObject.SetActive(true);
        EnchantUnit.gameObject.SetActive(true);*/

        /* _EffectImageRect.gameObject.SetActive(true);
         _EffectImageRect.localScale = Vector3.zero;*/

        Time.timeScale = 0f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        seq.Append(_EffectImageRect.DOScale(1.2f, 1.0f).SetEase(Ease.OutBack));
        seq.Append(_EffectImageRect.DOScale(1.0f, 0.7f).SetEase(Ease.OutQuart));

        seq.AppendCallback(() => {
            joker1.PlayJokerEffect(null);
            joker2.PlayJokerEffect(null);
        });
        seq.AppendInterval(1.0f);

        seq.AppendCallback(() => {
            EnchantUnit.PlayEnchantEffect(_enchantCount);
        });
        seq.AppendInterval(1.5f);

        seq.OnComplete(() =>
        {
            _EffectImageRect.gameObject.SetActive(false);
            EnchantUnit.gameObject.SetActive(false);
            Time.timeScale = 1f;
            onComplete?.Invoke();
        });
    }
}

