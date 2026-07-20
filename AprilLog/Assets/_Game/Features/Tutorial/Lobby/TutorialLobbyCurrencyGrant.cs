// 로비 튜토리얼용 재화(캐릭터 레벨업 골드/양피지, 아티팩트 강화석)를 기준값으로 맞춘다.
// 재화는 변경 즉시 저장되지만 튜토리얼 진행 단계는 저장되지 않으므로, 강제종료 후 재시작하면
// 진행은 0단계로 돌아가는데 재화만 소비된 상태로 남는다. 이를 막기 위해 튜토리얼이 시작될 때마다
// 재화를 정해진 기준값으로 세팅(Set)해 항상 같은 시작 상태를 보장한다.
// 세션 단위 플래그로 한 실행 안에서는 한 번만 세팅하고, 강제종료(새 프로세스)면 다시 세팅한다.

using System.Collections;
using UnityEngine;

public class TutorialLobbyCurrencyGrant : MonoBehaviour
{
    [Header("기준값")]
    [SerializeField] private int _grantGold = 20000;
    [SerializeField] private int _grantParchment = 50;
    [SerializeField] private int _grantUpgradeStone = 300;

    // 한 실행(프로세스) 안에서 이미 기준값을 맞췄는지. 씬 재로드에는 유지되고 앱 재시작이면 초기화된다.
    private static bool s_appliedThisSession;

    private void Start()
    {
        StartCoroutine(GrantWhenReady());
    }

    private IEnumerator GrantWhenReady()
    {
        if (s_appliedThisSession)
        {
            Debug.Log("[TutorialLobbyCurrencyGrant] 이번 실행에서 이미 기준값을 맞춰 다시 세팅하지 않습니다.");
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
                ApplyBaseline();
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

    private void ApplyBaseline()
    {
        ArtifactManager mgr = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;

        // 이전 진행이 남아 있어도 튜토리얼은 같은 시작 상태에서 진행되도록 아웃게임 상태를 먼저 초기화한다.
        // (이미 max로 저장된 아티팩트/레벨이 남으면 강화·레벨업 단계가 막힌다.)
        PlayerProgressModel progress = FindFirstObjectByType<PlayerProgressModel>();
        GameManager.Instance.ResetOutGameStateForTutorial(progress, mgr);

        GameManager.Instance.SetCurrency(_grantGold, _grantParchment);

        if (mgr != null)
            mgr.SetStone(Mathf.Max(0, _grantUpgradeStone));

        s_appliedThisSession = true;

        Debug.Log($"[TutorialLobbyCurrencyGrant] 아웃게임 상태 초기화 + 재화 기준값 세팅 완료 (골드 {_grantGold}, 양피지 {_grantParchment}, 강화석 {_grantUpgradeStone})");
    }

#if UNITY_EDITOR
    [ContextMenu("세션 세팅 플래그 초기화")]
    private void ResetSessionFlag()
    {
        s_appliedThisSession = false;
        Debug.Log("[TutorialLobbyCurrencyGrant] 세션 플래그를 초기화했습니다. 다음 로비 튜토리얼 시작 시 다시 기준값으로 세팅됩니다.");
    }
#endif
}
