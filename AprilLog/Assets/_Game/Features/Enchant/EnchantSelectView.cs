// 담당자 : 정승우
// 설명   : 인챈트 선택 팝업 View -- 선택지 표시 + 유저 입력

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

// 수정자 : 정승우
// 수정내용 : Model/UI 참조가 비어 있을 때 초기화를 건너뛰어 테스트 씬 NullReference 방지

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

// 수정자 : 김영찬
// 인챈트 선택 팝업 View -- 선택지 표시 + 유저 입력

using System;
using System.Collections.Generic;
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
    [SerializeField] private ScreenNavigator _navigator;

    [Header("UI")]
    [Tooltip("Horizontal Layout Group이 붙은 Content")]
    [SerializeField] private Transform _choiceContainer;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Button _rerollButton;
    
    [Header("UI 프리팹 세팅")]
    [Tooltip("EnchantCard 프리팹")]
    [SerializeField] private GameObject _cardPrefab;

    private EnchantSelectPresenter _presenter;
    private bool _isInitialized;
    
    // 런타임에 생성된 카드들을 추적하기 위한 캐시 리스트
    private List<GameObject> _spawnedCards = new List<GameObject>();

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            if (!HasRequiredReferences())
                return;

            _isInitialized = true;
            _presenter = new EnchantSelectPresenter(this, _enchantModel, Legacy_DataManager.Instance.CharacterRepo, _navigator);
            _skipButton.onClick.AddListener(() => OnSkipSelected?.Invoke());
        }
        
        // 팝업이 열릴 때 자동으로 프레젠터에게 선택지를 생성하라고 요청
        _presenter?.ShowSelection();
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void Show() => _navigator.ShowEnchantSelection();
    public void Hide() => _navigator.OnCloseButtonClick();
    /// <summary>
    /// Presenter가 계산된 인챈트 데이터를 넘겨주면, 프리팹을 생성하고 UI를 갱신합니다.
    /// </summary>
    public void SetChoices(Legacy_EnchantDisplayData[] choices)
    {
        if (choices == null || _choiceContainer == null || _cardPrefab == null) return;

        // 현재 카드 개수가 들어올 데이터보다 많을 때 파괴
        while (_spawnedCards.Count > choices.Length)
        {
            int lastIndex = _spawnedCards.Count - 1;
            Destroy(_spawnedCards[lastIndex]);
            _spawnedCards.RemoveAt(lastIndex);
        }

        // 현재 카드 개수가 들어올 데이터보다 적을 때 생성
        while (_spawnedCards.Count < choices.Length)
        {
            GameObject cardObj = Instantiate(_cardPrefab, _choiceContainer);
            _spawnedCards.Add(cardObj);
        }

        // 데이터 교체
        for (int i = 0; i < choices.Length; i++)
        {
            int index = i; 
            
            if (_spawnedCards[i].TryGetComponent<EnchantCardUI>(out var cardUI))
            {
                // 데이터 갱신
                cardUI.Setup(choices[i]);

                // 기존 이벤트 싹 지우기 (중복 클릭 방지)
                cardUI.OnCardClicked = null; 

                // 새로운 인덱스로 이벤트 연결
                cardUI.OnCardClicked += () =>
                {
                    DisableAllCardButtons();
                    SelectChoice(index); 
                    Hide();              
                };
            }
        }
    }
    public void ShowDeleteConfirm(Legacy_EnchantDisplayData toDelete, Legacy_EnchantDisplayData toAcquire)
    {
        
    }

    // View 버튼에서 호출
    public void SelectChoice(int index) => OnChoiceSelected?.Invoke(index);
    public void ConfirmDelete(int index) => OnDeleteConfirmed?.Invoke(index);

    // 멀티 터치 방지
    private void DisableAllCardButtons()
    {
        foreach (var card in _spawnedCards)
        {
            if (card != null && card.TryGetComponent<Button>(out var btn))
            {
                btn.interactable = false;
            }
        }
    }

    private bool HasRequiredReferences()
    {
        if (_enchantModel != null && _navigator != null && _skipButton != null && Legacy_DataManager.Instance.CharacterRepo != null && _cardPrefab != null)
            return true;

        Debug.LogWarning("[EnchantSelectView] 필수 참조가 비어 있어 초기화를 건너뜁니다.", this);
        return false;
    }
}
