//담당자: 조규민

using UnityEngine;

/// <summary>
/// 하우징 구매 테스트용 재화 지급을 담당합니다.
/// </summary>
public class HousingCurrencyTestController : MonoBehaviour
{
    [Header("테스트 재화 지급")]
    [Tooltip("꺼두면 Inspector 메뉴나 버튼에서 호출해도 재화를 지급하지 않습니다.")]
    [SerializeField] private bool _enableTestCurrencyGrant = true;
    [SerializeField] private int _testGoldAmount = 100000;
    [SerializeField] private int _testParchmentAmount = 100000;
    [SerializeField] private int _testDiamondAmount = 10000;

    [Header("대체 연결")]
    [Tooltip("GameManager가 없는 단독 UI 테스트에서만 사용할 CurrencyModel입니다.")]
    [SerializeField] private CurrencyModel _currencyModel;

    [ContextMenu("테스트/골드 추가")]
    public void AddTestGold()
    {
        AddTestCurrency(_testGoldAmount, 0, 0);
    }

    [ContextMenu("테스트/양피지 추가")]
    public void AddTestParchment()
    {
        AddTestCurrency(0, _testParchmentAmount, 0);
    }

    [ContextMenu("테스트/다이아 추가")]
    public void AddTestDiamond()
    {
        AddTestCurrency(0, 0, _testDiamondAmount);
    }

    [ContextMenu("테스트/전체 재화 추가")]
    public void AddAllTestCurrency()
    {
        AddTestCurrency(_testGoldAmount, _testParchmentAmount, _testDiamondAmount);
    }

    private void AddTestCurrency(int _gold, int _parchment, int _diamond)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!_enableTestCurrencyGrant)
        {
            Debug.Log("[HousingCurrencyTestController] 테스트 재화 지급이 비활성화되어 있습니다.", this);
            return;
        }

        if (!Application.isPlaying)
        {
            Debug.LogWarning("[HousingCurrencyTestController] 재화 지급은 Play Mode에서만 실행하세요.", this);
            return;
        }

        _gold = Mathf.Max(0, _gold);
        _parchment = Mathf.Max(0, _parchment);
        _diamond = Mathf.Max(0, _diamond);

        if (_gold == 0 && _parchment == 0 && _diamond == 0)
        {
            Debug.LogWarning("[HousingCurrencyTestController] 지급할 테스트 재화가 0입니다.", this);
            return;
        }

        if (TryAddByGameManager(_gold, _parchment, _diamond))
        {
            return;
        }

        if (TryAddByCurrencyModel(_gold, _parchment, _diamond))
        {
            return;
        }

        Debug.LogWarning("[HousingCurrencyTestController] GameManager 또는 CurrencyModel을 찾지 못해 테스트 재화를 지급하지 못했습니다.", this);
#else
        Debug.LogWarning("[HousingCurrencyTestController] 테스트 재화 지급은 Editor 또는 Development Build에서만 사용할 수 있습니다.", this);
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool TryAddByGameManager(int _gold, int _parchment, int _diamond)
    {
        if (GameManager.Instance == null)
        {
            return false;
        }

        if (_gold > 0 || _parchment > 0)
        {
            GameManager.Instance.AddCurrency(_gold, _parchment, "housing_test");
        }

        if (_diamond > 0)
        {
            GameManager.Instance.AddDiamond(_diamond, "housing_test");
        }

        Debug.Log($"[HousingCurrencyTestController] 테스트 재화 지급 완료 - 골드 {_gold}, 양피지 {_parchment}, 다이아 {_diamond}", this);
        return true;
    }

    private bool TryAddByCurrencyModel(int _gold, int _parchment, int _diamond)
    {
        CurrencyModel _resolvedCurrencyModel = ResolveCurrencyModel();

        if (_resolvedCurrencyModel == null)
        {
            return false;
        }

        if (_gold > 0)
        {
            _resolvedCurrencyModel.AddGold(_gold);
        }

        if (_parchment > 0)
        {
            _resolvedCurrencyModel.AddParchment(_parchment);
        }

        if (_diamond > 0)
        {
            _resolvedCurrencyModel.AddDiamond(_diamond);
        }

        Debug.Log($"[HousingCurrencyTestController] CurrencyModel 테스트 재화 지급 완료 - 골드 {_gold}, 양피지 {_parchment}, 다이아 {_diamond}", this);
        return true;
    }

    private CurrencyModel ResolveCurrencyModel()
    {
        if (_currencyModel != null)
        {
            return _currencyModel;
        }

        _currencyModel = FindFirstObjectByType<CurrencyModel>(FindObjectsInactive.Include);
        return _currencyModel;
    }
#endif
}
