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
    private Sequence _seq;

    public void SetEnchantCount(int count)
    {
        _enchantCount = count;
    }

    // 연출 도중 오브젝트가 꺼지거나 씬을 떠나면 DOTween OnComplete가 영영 오지 않아
    // static IsEffectPlaying이 true로 고착된다(이후 일시정지 해제 불능). 여기서 강제 복구한다.
    private void OnDisable()
    {
        if (_seq != null && _seq.IsActive()) _seq.Kill();
        _seq = null;

        if (ScreenNavigator.IsEffectPlaying)
        {
            ScreenNavigator.IsEffectPlaying = false;
            if (!ScreenNavigator.IsMenuOpen)
                Time.timeScale = 1f;
        }
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
        _seq = seq;

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
                    Debug.Log("[Reward] ��Ŀ ���� �Ϸ�");
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

