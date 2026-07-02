// 로비 튜토리얼 시작 시 캐릭터 레벨업·아티팩트 강화에 필요한 재화를 한 번 넉넉히 지급한다.
// 골드/양피지(캐릭터 레벨업), 강화석(아티팩트 강화)을 지급하고, PlayerPrefs로 1회만 지급되게 막는다.
// 아티팩트 인장(기본/돌파용 중복)은 뽑기 단계에서 별도 지급된다.

using UnityEngine;

public class TutorialLobbyCurrencyGrant : MonoBehaviour
{
    // 튜토리얼 소요량 : 아티팩트 5레벨 강화(골드 6,000/강화석 120) + 돌파 1회(같은 장비 소모, 재화 0)
    //                 + 캐릭터 레벨업 1회(골드 50/양피지 5). 여기에 여유분을 더해 지급한다.
    [Header("지급량 (소요량 + 여유분)")]
    [SerializeField] private int _grantGold = 20000;
    [SerializeField] private int _grantParchment = 50;
    [SerializeField] private int _grantUpgradeStone = 300;

    [Header("1회 지급 가드")]
    [Tooltip("이 키로 지급 여부를 저장한다. 테스트로 다시 지급하려면 비활성 상태에서 컨텍스트 메뉴로 초기화")]
    [SerializeField] private string _grantedPrefKey = "tutorial_lobby_currency_granted";

    private void Start()
    {
        var tm = TutorialManager.Instance;
        if (tm == null || !tm.IsRunning) return;
        if (PlayerPrefs.GetInt(_grantedPrefKey, 0) == 1) return;

        // 영속 재화는 GameManager를 거친다. 부트 미경유 단독 씬 테스트면 지급하지 않는다.
        if (GameManager.Instance == null) return;

        GameManager.Instance.AddCurrency(_grantGold, _grantParchment, "튜토리얼 로비 재화 지급");

        var mgr = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
        if (mgr != null && _grantUpgradeStone > 0)
            mgr.AddStone(_grantUpgradeStone);

        PlayerPrefs.SetInt(_grantedPrefKey, 1);
        PlayerPrefs.Save();
        Debug.Log($"[TutorialLobbyCurrencyGrant] 재화 지급 완료 (골드 +{_grantGold}, 양피지 +{_grantParchment}, 강화석 +{_grantUpgradeStone})");
    }

#if UNITY_EDITOR
    [ContextMenu("지급 플래그 초기화")]
    private void ResetGrantFlag()
    {
        PlayerPrefs.DeleteKey(_grantedPrefKey);
        PlayerPrefs.Save();
        Debug.Log("[TutorialLobbyCurrencyGrant] 지급 플래그를 초기화했습니다. 다음 로비 튜토리얼 시작 시 다시 지급됩니다.");
    }
#endif
}
