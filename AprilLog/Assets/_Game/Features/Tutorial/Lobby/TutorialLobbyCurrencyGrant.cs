// 로비 튜토리얼 시작 시 캐릭터 레벨업/아티팩트 강화에 필요한 재화를 한 번 지급한다.
// 골드/양피지(캐릭터 레벨업), 강화석(아티팩트 강화)을 지급하고 PlayerPrefs로 중복 지급을 막는다.

using System.Collections;
using UnityEngine;

public class TutorialLobbyCurrencyGrant : MonoBehaviour
{
    [Header("지급량")]
    [SerializeField] private int _grantGold = 20000;
    [SerializeField] private int _grantParchment = 50;
    [SerializeField] private int _grantUpgradeStone = 300;

    [Header("1회 지급 가드")]
    [Tooltip("이 키로 지급 여부를 저장한다. 테스트로 다시 지급하려면 컨텍스트 메뉴로 초기화한다.")]
    [SerializeField] private string _grantedPrefKey = "tutorial_lobby_currency_granted";

    private void Start()
    {
        StartCoroutine(GrantWhenReady());
    }

    private IEnumerator GrantWhenReady()
    {
        if (PlayerPrefs.GetInt(_grantedPrefKey, 0) == 1)
        {
            Debug.Log("[TutorialLobbyCurrencyGrant] 이미 지급된 상태라 재지급하지 않습니다.");
            yield break;
        }

        const float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool tutorialReady = TutorialManager.Instance != null && TutorialManager.Instance.IsRunning;
            bool gameReady = GameManager.Instance != null;
            bool artifactReady = _grantUpgradeStone <= 0
                || (GameStateManager.Instance != null && GameStateManager.Instance.ArtifactManager != null);

            if (tutorialReady && gameReady && artifactReady)
            {
                Grant();
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning(
            "[TutorialLobbyCurrencyGrant] 재화 지급 준비 실패. " +
            $"TutorialRunning={TutorialManager.Instance != null && TutorialManager.Instance.IsRunning}, " +
            $"GameManager={GameManager.Instance != null}, " +
            $"ArtifactManager={GameStateManager.Instance != null && GameStateManager.Instance.ArtifactManager != null}",
            this);
    }

    private void Grant()
    {
        GameManager.Instance.AddCurrency(_grantGold, _grantParchment, "튜토리얼 로비 재화 지급");

        ArtifactManager mgr = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
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
