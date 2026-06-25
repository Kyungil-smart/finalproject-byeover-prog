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

// 2차 수정자 : 조규민
// 수정 내용 : 인게임 인챈트 선택 팝업 리롤 횟수 설정, 남은 횟수 표시 갱신, 리롤 아이콘/텍스트 터치 가로막음 방지 추가
//            카드별 리롤 버튼과 특정 카드만 리롤하는 이벤트 연결, Viewport Mask 외부 오버레이 배치 추가
//            씬 RerollBoundary를 남은 리롤 횟수 표시 전용으로 변경

// 3차 수정자 : 김영찬
// 수정 내용 : 기존 인첸트 선택 로직과 신규 인첸트 선택 로직을 선택해 게임에 적용 가능 하도록 수정
//           이 기능은 인스팩터에서 작동 되며, 게임 씬 구동 전에 선택 해둬야됨. (빌드나 에디터 실행 도중에는 스위치 되지 않음)

using System;
using System.Collections.Generic;
using TMPro;
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
    public event Action OnRerollSelected;
    public event Action<int> OnCardRerollSelected;

    [Header("인챈트 등장 확률 설정 및 출현 로직 결정")]
    [Tooltip("인챈트 등장 확률 설정")]
    [SerializeField] private EnchantProbabilityConfig _probabilityConfig;
    [Tooltip("인챈트 등장 턴 순서 설정 (스킬/스텟 분리 출현 선택 시)")]
    [SerializeField] private EnchantSequenceConfig _sequenceConfig;
    [Tooltip("스킬/스텟이 분리 출현하는 시퀀스 선택 / True = 스킬/스텟이 순서에 맞춰 분리되어 출현")]
    [SerializeField] private bool _useSequence;
    
    [Header("참조")]
    [SerializeField] private EnchantModel _model;
    [SerializeField] private EnchantUIModel _modelUI;
    [SerializeField] private ScreenNavigator _navigator;
    [SerializeField] private EnchantChangeView _changeView;
    [SerializeField] private EnchantListView _listView;

    [Header("UI")]
    [Tooltip("Horizontal Layout Group이 붙은 Content")]
    [SerializeField] private Transform _choiceContainer;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Button _rerollButton;
    [SerializeField] private TMP_Text _rerollCountText;

    [Header("리롤 오버레이")]
    [Tooltip("비워두면 EnchantSelectCanvas 아래에 Viewport Mask 영향을 받지 않는 오버레이를 자동 생성합니다.")]
    [SerializeField] private RectTransform _rerollOverlay;
    
    [Header("리롤 설정")]
    [Tooltip("인챈트 선택 팝업이 열릴 때 제공할 새로고침 횟수")]
    [SerializeField] private int _rerollCount = 1;
    
    [Header("UI 프리팹 세팅")]
    [Tooltip("EnchantCard 프리팹")]
    [SerializeField] private GameObject _cardPrefab;

    private IEnchantSelectPresenter _selectPresenter;
    private EnchantChangePresenter _changePresenter;
    
    private bool _isInitialized;
    
    // 런타임에 생성된 카드들을 추적하기 위한 캐시 리스트
    private List<GameObject> _spawnedCards = new List<GameObject>();
    private TMP_Text _resolvedRerollCountText;
    private bool _currentRerollAvailable;
    private int _currentRerollRemaining;
    private ScrollRect _choiceScrollRect;

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            if (!HasRequiredReferences())
                return;
            
            _listView.Init();
            
            _changePresenter = new EnchantChangePresenter(_changeView, _model, _modelUI, _navigator);
            
            // 테스트(Skill_TEST2 / Skill_TEST3) 씬에서만 새로고침 무한 — 스킬 뽑기 테스트용. (-1 = 무한 약속) 일반 씬은 Inspector _rerollCount 그대로.
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool unlimitedReroll = activeScene.Contains("TEST2") || activeScene.Contains("테스트2")
                                || activeScene.Contains("TEST3") || activeScene.Contains("테스트3");

            if (_useSequence)
            {
                _selectPresenter = new EnchantSequenceSelectPresenter(
                    this, 
                    _model, 
                    DataManager.Instance.SpellRepo, 
                    _navigator, 
                    _probabilityConfig, 
                    _sequenceConfig, 
                    _changePresenter,
                    unlimitedReroll ? -1 : _rerollCount
                );
            }
            else
            {
                _selectPresenter = new EnchantSelectPresenter(
                    this,
                    _model,
                    DataManager.Instance.SpellRepo,
                    _navigator,
                    _probabilityConfig,
                    _changePresenter,
                    unlimitedReroll ? -1 : _rerollCount
                );
            }
            
            _skipButton.onClick.AddListener(() => OnSkipSelected?.Invoke());
            if (_rerollButton != null)
            {
                ConfigureRerollCountDisplay();
                SetLegacyRerollVisible(true);
            }
            
            _isInitialized = true;
        }

        EnsureRerollOverlay();
        SubscribeScrollPosition();
        
        // 팝업이 열릴 때 자동으로 프레젠터에게 선택지를 생성하라고 요청
        _selectPresenter?.ShowSelection();
    }

    private void OnDestroy()
    {
        UnsubscribeScrollPosition();
        _selectPresenter?.Dispose();
        _changePresenter?.Dispose();
    }

    public void Show() => _navigator.ShowEnchantSelection();
    public void Hide() => _navigator.OnCloseButtonClick();
    
    /// <summary>
    /// Presenter가 계산된 인챈트 데이터를 넘겨주면, 프리팹을 생성하고 UI를 갱신합니다.
    /// </summary>
    public void SetChoices(EnchantDisplayData[] choices)
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

            // 이전 선택에서 DisableAllCardButtons()로 꺼둔 버튼을 다시 켠다.
            // (카드를 재사용하므로, 안 켜면 두 번째 팝업부터 클릭이 안 먹힘)
            if (_spawnedCards[i].TryGetComponent<Button>(out var cardBtn))
                cardBtn.interactable = true;

            if (_spawnedCards[i].TryGetComponent<EnchantCardUI>(out var cardUI))
            {
                // 데이터 갱신
                cardUI.Setup(choices[i]);

                // 기존 이벤트 싹 지우기 (중복 클릭 방지)
                cardUI.OnCardClicked = null; 
                cardUI.OnRerollClicked = null;

                // 새로운 인덱스로 이벤트 연결
                cardUI.OnCardClicked += () =>
                {
                    DisableAllCardButtons();
                    SelectChoice(index); 
                    Hide();              
                };

                cardUI.OnRerollClicked += () => OnCardRerollSelected?.Invoke(index);
                cardUI.AttachRerollOverlay(_rerollOverlay);
                cardUI.SetRerollState(_currentRerollAvailable, _currentRerollRemaining);
            }
        }

        RefreshRerollPositions();
    }

    // View 버튼에서 호출
    public void SelectChoice(int index) => OnChoiceSelected?.Invoke(index);
    public void ConfirmDelete(int index) => OnDeleteConfirmed?.Invoke(index);

    /// <summary>새로고침(리롤) 버튼 상태 갱신. available=false면 버튼을 숨김(일반 씬). remaining=남은 횟수(0이면 비활성).</summary>
    public void SetRerollAvailable(bool available, int remaining)
    {
        _currentRerollAvailable = available;
        _currentRerollRemaining = remaining;

        SetLegacyRerollVisible(true);
        UpdateRerollCountText(remaining);

        foreach (GameObject card in _spawnedCards)
        {
            if (card != null && card.TryGetComponent<EnchantCardUI>(out var cardUI))
            {
                cardUI.SetRerollState(available, remaining);
            }
        }
    }

    // 멀티 터치 방지
    private void DisableAllCardButtons()
    {
        foreach (var card in _spawnedCards)
        {
            if (card == null) continue;

            if (card.TryGetComponent<Button>(out var btn))
            {
                btn.interactable = false;
            }

            if (card.TryGetComponent<EnchantCardUI>(out var cardUI))
            {
                cardUI.SetSelectInteractable(false);
                cardUI.SetRerollState(false, _currentRerollRemaining);
            }
        }
    }

    private void EnsureRerollOverlay()
    {
        if (_rerollOverlay != null)
        {
            _rerollOverlay.SetAsLastSibling();
            return;
        }

        Transform existing = transform.Find("RerollOverlay");
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            _rerollOverlay = existingRect;
            _rerollOverlay.SetAsLastSibling();
            return;
        }

        GameObject overlayObject = new GameObject("RerollOverlay", typeof(RectTransform));
        overlayObject.layer = gameObject.layer;
        _rerollOverlay = overlayObject.GetComponent<RectTransform>();
        _rerollOverlay.SetParent(transform, false);
        _rerollOverlay.anchorMin = Vector2.zero;
        _rerollOverlay.anchorMax = Vector2.one;
        _rerollOverlay.pivot = new Vector2(0.5f, 0.5f);
        _rerollOverlay.anchoredPosition = Vector2.zero;
        _rerollOverlay.sizeDelta = Vector2.zero;
        _rerollOverlay.SetAsLastSibling();
    }

    private void SubscribeScrollPosition()
    {
        if (_choiceScrollRect != null || _choiceContainer == null) return;

        _choiceScrollRect = _choiceContainer.GetComponentInParent<ScrollRect>();
        if (_choiceScrollRect != null)
        {
            _choiceScrollRect.onValueChanged.AddListener(HandleScrollPositionChanged);
        }
    }

    private void UnsubscribeScrollPosition()
    {
        if (_choiceScrollRect == null) return;

        _choiceScrollRect.onValueChanged.RemoveListener(HandleScrollPositionChanged);
        _choiceScrollRect = null;
    }

    private void HandleScrollPositionChanged(Vector2 _)
    {
        RefreshRerollPositions();
    }

    private void RefreshRerollPositions()
    {
        if (_choiceContainer is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        Canvas.ForceUpdateCanvases();
        foreach (GameObject card in _spawnedCards)
        {
            if (card != null && card.TryGetComponent(out EnchantCardUI cardUI))
            {
                cardUI.RefreshRerollPosition();
            }
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || _rerollOverlay == null) return;

        RefreshRerollPositions();
    }

    private void SetLegacyRerollVisible(bool visible)
    {
        if (_rerollButton == null) return;

        GameObject rerollRoot = GetRerollRoot();
        if (rerollRoot != null)
            rerollRoot.SetActive(visible);
        else
            _rerollButton.gameObject.SetActive(visible);
    }

    private GameObject GetRerollRoot()
    {
        if (_rerollButton == null || _rerollButton.transform.parent == null)
            return null;

        return _rerollButton.transform.parent.gameObject;
    }

    private void ConfigureRerollCountDisplay()
    {
        GameObject rerollRoot = GetRerollRoot();
        if (rerollRoot == null) return;

        _rerollButton.enabled = false;

        Graphic[] graphics = rerollRoot.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null) continue;

            graphic.raycastTarget = false;
        }
    }

    private void UpdateRerollCountText(int remaining)
    {
        TMP_Text countText = ResolveRerollCountText();
        if (countText == null) return;

        string remainingText = remaining < 0 ? "INF" : Mathf.Max(0, remaining).ToString();
        countText.text = $"X {remainingText}";
    }

    private TMP_Text ResolveRerollCountText()
    {
        if (_rerollCountText != null)
            return _rerollCountText;

        if (_resolvedRerollCountText != null)
            return _resolvedRerollCountText;

        GameObject rerollRoot = GetRerollRoot();
        if (rerollRoot == null)
            return null;

        TMP_Text[] texts = rerollRoot.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null && text.name.Contains("RerollCountText"))
            {
                _resolvedRerollCountText = text;
                return _resolvedRerollCountText;
            }
        }

        if (texts.Length > 0)
            _resolvedRerollCountText = texts[0];

        return _resolvedRerollCountText;
    }

    private bool HasRequiredReferences()
    {
        if (_model != null && _navigator != null && _skipButton != null && Legacy_DataManager.Instance.CharacterRepo != null && _cardPrefab != null)
            return true;

        Debug.LogWarning("[EnchantSelectView] 필수 참조가 비어 있어 초기화를 건너뜁니다.", this);
        return false;
    }
}
