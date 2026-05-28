using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명 : XP게이지 연출

public class LobbyLevelGaugeUI : MonoBehaviour
{
    [Header("레벨 UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image expGauge;

    [Header("연출 설정")]
    [SerializeField] private float gaugeTweenDuration = 0.35f;

    private Tween _gaugeTween;

    public void SetLevelExp(int level, int currentExp, int requiredExp, bool animate)
    {
        level = Mathf.Max(1, level);
        requiredExp = Mathf.Max(1, requiredExp);

        float targetFillAmount = Mathf.Clamp01((float)currentExp / requiredExp);

        if (levelText != null)
            levelText.text = $"Lv.{level}";

        if (expGauge == null)
            return;

        _gaugeTween?.Kill();

        if (animate)
        {
            _gaugeTween = expGauge
                .DOFillAmount(targetFillAmount, gaugeTweenDuration)
                .SetEase(Ease.OutCubic);
        }
        else
        {
            expGauge.fillAmount = targetFillAmount;
        }
    }

    private void OnDestroy()
    {
        _gaugeTween?.Kill();
    }
}