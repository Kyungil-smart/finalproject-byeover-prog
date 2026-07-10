using DG.Tweening;
using UnityEngine;
using System;

public class EliteRewardEffect : MonoBehaviour
{
    [SerializeField] private GameObject _container;
    [SerializeField] private RectTransform _EffectImageRect;
    public JokerSystem jokerSystem;

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
        ScreenNavigator.IsEffectPlaying = true;
        Time.timeScale = 0f;
        _container.SetActive(true);

        _EffectImageRect.localScale = Vector3.zero;

        EnchantUnit.ResetToDefault();
        joker1.gameObject.SetActive(true);
        joker2.gameObject.SetActive(true);
        EnchantUnit.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        seq.Append(_EffectImageRect.DOScale(1.2f, 1.0f).SetEase(Ease.OutBack));
        seq.Append(_EffectImageRect.DOScale(1.0f, 0.7f).SetEase(Ease.OutQuart));

        seq.AppendCallback(() =>
        {
            joker1.PlayJokerEffect(null);
            joker2.PlayJokerEffect(null);
        });
        seq.AppendInterval(1.0f);

        seq.AppendCallback(() =>
        {
            EnchantUnit.PlayEnchantEffect(_enchantCount, () =>
            {
                _container.SetActive(false);

                ScreenNavigator.IsEffectPlaying = false;

                if (jokerSystem != null)
                {
                    jokerSystem.RestoreJokerImages();
                    Debug.Log("[Reward] 조커 충전 완료");
                }

                if (!ScreenNavigator.IsMenuOpen && !ScreenNavigator.IsEffectPlaying)
                {
                    Time.timeScale = 1.0f;
                }

                onComplete?.Invoke();
            });
        });
    }
}

