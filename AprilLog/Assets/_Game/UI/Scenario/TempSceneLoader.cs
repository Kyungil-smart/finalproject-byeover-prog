// 담당자 : 홍정옥
// 설명   : [임시] 버튼 onClick으로 지정 씬을 로드 (기획자 테스트 빌드용, 나중에 삭제)

using UnityEngine;
using UnityEngine.SceneManagement;

public class TempSceneLoader : MonoBehaviour
{
    [Tooltip("로드할 씬 이름 (Build Settings에 포함되어 있어야 함)")]
    [SerializeField] private string _sceneName = "_Story";

    /// <summary>인스펙터의 _sceneName 씬을 로드 (버튼 onClick에 연결)</summary>
    public void LoadScene()
    {
        if (string.IsNullOrEmpty(_sceneName))
        {
            Debug.LogWarning("[TempSceneLoader] 씬 이름이 비어있습니다.", this);
            return;
        }
        // 현재 유일한 사용처가 로비 PLAY 버튼이라 여기서 재생. 로비 스테이지 선택이 정식 배선되면
        // 이 파일 삭제와 함께 스타트 버튼 핸들러로 옮길 것. (SFX 가이드 아웃게임 9: 인게임 입장)
        AudioManager.Play(SfxId.EnterInGame);
        SceneManager.LoadScene(_sceneName);
    }

    /// <summary>씬 이름을 직접 지정해 로드</summary>
    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
}
