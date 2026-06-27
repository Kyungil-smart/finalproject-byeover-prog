using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 재화 1종을 연동하는 재사용 컴포넌트
//          표시 위치가 화면마다 다르므로, 재화 텍스트가 필요한 곳마다 이 컴포넌트를 하나씩 붙이고
//          Kind 로 어떤 재화를 보여줄지 고른다
//
//          데이터 출처 :
//           - Gold / Parchment      : CurrencyModel(→ GameManager). 변경 시 OnCurrencyChanged 로 자동 갱신.
//           - ArtifactUpgradeStone  : ArtifactManager.UpgradeStone.   변경 시 OnInventoryUpdated 로 자동 갱신.
//           - LegendaryShard        : ArtifactManager.LegendaryShard. 변경 시 OnInventoryUpdated 로 자동 갱신.
//           - Stamina(행동력)       : StaminaModel.Current/Max.        변경 시 OnStaminaChanged 로 자동 갱신('현재/최대' 표시).
//           - Diamond               : CurrencyModel.Diamond(→ GameManager/CloudData, 영속). 변경 시 OnCurrencyChanged 로 자동 갱신.
//           - GachaTicket           : ExtraCurrencyModel.GachaTicket. 변경 시 OnChanged 로 자동 갱신(아직 임시 보관, 인벤토리 이관 예정).
[DisallowMultipleComponent]
public class CurrencyTextView : MonoBehaviour
{
    public enum CurrencyKind
    {
        Gold,                 // 골드
        Parchment,            // 양피지
        Diamond,              // 다이아
        GachaTicket,          // 뽑기 티켓
        ArtifactUpgradeStone, // 아티팩트 강화석 (아티팩트 상세창의 업그레이드 재화)
        LegendaryShard,       // 레전더리 조각
        Stamina               // 행동력(스태미나) — '현재/최대' 형식으로 표시
    }

    [Header("표시할 재화")]
    [SerializeField] private CurrencyKind _kind = CurrencyKind.Gold;

    [Header("UI 연결")]
    [Tooltip("재화 보유량을 표시할 텍스트")]
    [SerializeField] private TMP_Text _amountText;
    [Tooltip("재화 아이콘(선택). 비우면 아이콘은 건드리지 않는다.")]
    [SerializeField] private Image _iconImage;
    [Tooltip("이 재화의 아이콘 스프라이트(선택). _iconImage 가 있으면 활성화 시 이 스프라이트로 교체한다.")]
    [SerializeField] private Sprite _iconSprite;

    [Header("표시 형식")]
    [Tooltip("켜면 1000 이상을 1.2k 형태로 축약(CurrencyModel.FormatAmount). 끄면 원래 숫자 그대로.")]
    [SerializeField] private bool _useShortFormat = false;
    [Tooltip("숫자 앞/뒤에 붙일 접두/접미사(예: 접두 'x', 접미 '개'). 선택.")]
    [SerializeField] private string _prefix = "";
    [SerializeField] private string _suffix = "";
    [Tooltip("Stamina(행동력)일 때만 사용. 켜면 '현재/최대'(예: 120/999), 끄면 현재값만 표시.")]
    [SerializeField] private bool _staminaShowMax = true;

    [Header("데이터 소스 (비우면 자동 탐색)")]
    [SerializeField] private CurrencyModel _currencyModel;
    [Tooltip("Stamina(행동력) 표시용. 비우면 자동 탐색.")]
    [SerializeField] private StaminaModel _staminaModel;
    [Tooltip("Diamond / GachaTicket 표시용. 비우면 자동 탐색.")]
    [SerializeField] private ExtraCurrencyModel _extraCurrencyModel;

    private ArtifactManager ArtifactManager =>
        GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;

    private void Awake()
    {
        if (_currencyModel == null)
            _currencyModel = FindFirstObjectByType<CurrencyModel>(FindObjectsInactive.Include);
        if (_staminaModel == null)
            _staminaModel = FindFirstObjectByType<StaminaModel>(FindObjectsInactive.Include);
        if (_extraCurrencyModel == null)
            _extraCurrencyModel = FindFirstObjectByType<ExtraCurrencyModel>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        ApplyIcon();
        Subscribe(true);
        Refresh();
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    // 재화별 변경 이벤트 구독/해제. 이벤트가 없는 재화(다이아/티켓)는 구독할 게 없다.
    private void Subscribe(bool on)
    {
        switch (_kind)
        {
            case CurrencyKind.Gold:
            case CurrencyKind.Parchment:
            case CurrencyKind.Diamond:   // 다이아도 CurrencyModel 로 통합 → 동일 이벤트로 갱신
                if (_currencyModel != null)
                {
                    if (on) _currencyModel.OnCurrencyChanged += HandleCurrencyChanged;
                    else _currencyModel.OnCurrencyChanged -= HandleCurrencyChanged;
                }
                break;

            case CurrencyKind.ArtifactUpgradeStone:
            case CurrencyKind.LegendaryShard:
                ArtifactManager mgr = ArtifactManager;
                if (mgr != null)
                {
                    if (on) mgr.OnInventoryUpdated += Refresh;
                    else mgr.OnInventoryUpdated -= Refresh;
                }
                break;

            case CurrencyKind.Stamina:
                if (_staminaModel != null)
                {
                    if (on) _staminaModel.OnStaminaChanged += HandleStaminaChanged;
                    else _staminaModel.OnStaminaChanged -= HandleStaminaChanged;
                }
                break;

            case CurrencyKind.GachaTicket:
                if (_extraCurrencyModel != null)
                {
                    if (on) _extraCurrencyModel.OnChanged += Refresh;
                    else _extraCurrencyModel.OnChanged -= Refresh;
                }
                break;
        }
    }

    private void HandleCurrencyChanged(int gold, int parchment) => Refresh();
    private void HandleStaminaChanged(int current, int max) => Refresh();

    // 외부(팝업 열기 등)에서 강제로 다시 그릴 때 호출해도 된다.
    public void Refresh()
    {
        if (_amountText == null)
            return;

        // 행동력은 회복형 자원이라 '현재/최대' 형식으로 표시(옵션).
        if (_kind == CurrencyKind.Stamina)
        {
            int current = _staminaModel != null ? _staminaModel.Current : 0;
            int max = _staminaModel != null ? _staminaModel.Max : 0;
            string staminaBody = _staminaShowMax ? $"{current}/{max}" : current.ToString();
            _amountText.text = $"{_prefix}{staminaBody}{_suffix}";
            return;
        }

        int amount = ResolveAmount();
        string body = _useShortFormat ? CurrencyModel.FormatAmount(amount) : amount.ToString();
        _amountText.text = $"{_prefix}{body}{_suffix}";
    }

    // 재화 종류별 현재 보유량을 데이터 소스에서 읽어온다.
    private int ResolveAmount()
    {
        switch (_kind)
        {
            case CurrencyKind.Gold:
                return _currencyModel != null ? _currencyModel.Gold
                     : (GameManager.Instance != null ? GameManager.Instance.Gold : 0);

            case CurrencyKind.Parchment:
                return _currencyModel != null ? _currencyModel.Parchment
                     : (GameManager.Instance != null ? GameManager.Instance.Parchment : 0);

            case CurrencyKind.ArtifactUpgradeStone:
                return ArtifactManager != null ? ArtifactManager.UpgradeStone : 0;

            case CurrencyKind.LegendaryShard:
                return ArtifactManager != null ? ArtifactManager.LegendaryShard : 0;

            case CurrencyKind.Diamond:
                return _currencyModel != null ? _currencyModel.Diamond
                     : (GameManager.Instance != null ? GameManager.Instance.Diamond : 0);

            case CurrencyKind.GachaTicket:
                if (_extraCurrencyModel != null) return _extraCurrencyModel.GachaTicket;
                WarnNoExtraModelOnce();
                return 0;

            default:
                return 0;
        }
    }

    private void ApplyIcon()
    {
        if (_iconImage != null && _iconSprite != null)
            _iconImage.sprite = _iconSprite;
    }

    private bool _warned;
    private void WarnNoExtraModelOnce()
    {
        if (_warned) return;
        _warned = true;
        Debug.LogWarning($"[CurrencyTextView] '{_kind}'(뽑기 티켓) 표시에 ExtraCurrencyModel 이 씬에 없어 0 으로 표시됩니다. " +
                         "ExtraCurrencyModel 을 씬에 배치하거나 _extraCurrencyModel 에 연결하세요.", this);
    }
}
