// 담당자 : 정승우
// 설명   : 인챈트 선택 팝업 View -- 선택지 표시 + 유저 입력

using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨업 시 뜨는 인챈트 선택 팝업 표시. 로직 없음.
/// 비활성 상태로 시작하므로 OnEnable에서 Lazy 초기화.
/// </summary>
public class EnchantSelectView : MonoBehaviour, IEnchantSelectView
{
    public event Action<int> OnChoiceSelected;
    public event Action OnSkipSelected;
    public event Action<int> OnDeleteConfirmed;

    [Header("참조")]
    [SerializeField] private EnchantModel _enchantModel;
    [SerializeField] private CharacterRepo _characterRepo;
    [SerializeField] private ScreenNavigator _navigator;

    [Header("UI")]
    [SerializeField] private Transform _choiceContainer;
    [SerializeField] private GameObject _deleteConfirmPanel;
    [SerializeField] private Button _skipButton;

    private EnchantSelectPresenter _presenter;
    private bool _isInitialized;

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            _presenter = new EnchantSelectPresenter(this, _enchantModel, _characterRepo, _navigator);
            _skipButton.onClick.AddListener(() => OnSkipSelected?.Invoke());
        }
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
    public void SetChoices(EnchantDisplayData[] choices) { /* 선택 카드 세팅 */ }
    public void ShowDeleteConfirm(EnchantDisplayData toDelete, EnchantDisplayData toAcquire)
    {
        if (_deleteConfirmPanel != null) _deleteConfirmPanel.SetActive(true);
    }

    // View 버튼에서 호출
    public void SelectChoice(int index) => OnChoiceSelected?.Invoke(index);
    public void ConfirmDelete(int index) => OnDeleteConfirmed?.Invoke(index);
}
