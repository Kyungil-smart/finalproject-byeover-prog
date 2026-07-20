using UnityEngine;

public class EnchantPopupManager : MonoBehaviour
{
    [SerializeField] private EliteMonsterReward _rewardSystem;
    [SerializeField] private EliteRewardEffect _rewardEffect;
    [SerializeField] private UnitVisualController _normalUnit;
    [SerializeField] private EnchantSelectView _view;
    [SerializeField] private EnchantChangeView _changeView;

    private int _remainingPopups = 0;
    private bool _isEffectPlaying = false;

    private void Awake()
    {
        if (_rewardSystem != null)
            _rewardSystem.Manager = this;
    }

    private void OnEnable()
    {
        if (_view != null) _view.OnChoiceSelected += HandleChoiceSelected;
    }

    private void OnDisable()
    {
        if (_view != null) _view.OnChoiceSelected -= HandleChoiceSelected;
    }

    public void StartRewardSequence()
    {
        _isEffectPlaying = true;
        float rand = Random.value;
        if (rand < 0.1f) _remainingPopups = 3;
        else if (rand < 0.4f) _remainingPopups = 2;
        else _remainingPopups = 1;

        Debug.Log($"[EnchantPopup] 확률 결과: {rand:F2} (0~0.1: 10%, 0.1~0.4: 30%, 0.4~1.0: 60%) | 생성된 팝업 횟수: {_remainingPopups}");



        if (_rewardEffect != null)
        {
            _rewardEffect.SetEnchantCount(_remainingPopups);

            _rewardEffect.PlayRewardEffect(() =>
            {
                _isEffectPlaying = false;
                ShowNextPopup();
            });
        }

        else
        {
            _isEffectPlaying = false;
            ShowNextPopup();
        }
    }

    private void HandleChoiceSelected(int index)
    {
        StartCoroutine(ShowNextPopupRoutine());
    }
    private System.Collections.IEnumerator ShowNextPopupRoutine()
    {
        _view.Hide();
        yield return null;

        ShowNextPopup();
    }

    public bool IsChangePopupActive()
    {
        return _changeView != null && _changeView.gameObject.activeSelf;
    }

    public void ShowNextPopup()
    {
        if (_isEffectPlaying)
        {
            return;
        }

        if (IsChangePopupActive())
        {
            return;
        }

        if (_remainingPopups > 0)
        {
            _remainingPopups--;
            _rewardSystem.OpenEnchantPopup();
        }
    }

    public void OnChangeCompleted()
    {
        if (_changeView != null)
            _changeView.gameObject.SetActive(false);

        StartCoroutine(DelayShowNextPopup());
    }

    private System.Collections.IEnumerator DelayShowNextPopup()
    {
        yield return null;
        ShowNextPopup();
    }
}
