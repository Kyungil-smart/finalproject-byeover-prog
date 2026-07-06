//담당자: 조규민
//스토리 다시보기 팝업 자동 탐색 시 씬 전체 Transform 배열 생성을 제거

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 책장에서 스토리 다시보기 팝업을 엽니다.
/// </summary>
// 책장 선택 입력을 다시보기 컨텍스트에 저장하고 시나리오 선택 팝업 열기
public class HousingBookshelfReplayBinder : MonoBehaviour
{
    [Header("책장 버튼")]
    [Tooltip("스토리 다시보기 팝업을 열 책장 버튼입니다.")]
    [SerializeField] private Button _bookshelfButton;

    [Header("스토리 다시보기")]
    [Tooltip("책장에서 열 스토리 다시보기 팝업입니다.")]
    [SerializeField] private ReplayStoryPopup _replayStoryPopup;

    private void Awake()
    {
        ResolveMissingReferences();
    }

    // 책장 버튼 클릭 이벤트 중복 제거 후 등록
    private void OnEnable()
    {
        ResolveMissingReferences();

        if (_bookshelfButton == null)
        {
            Debug.LogWarning("[HousingBookshelfReplayBinder] 책장 버튼이 연결되지 않았습니다.", this);
            return;
        }

        _bookshelfButton.onClick.RemoveListener(OpenReplayStoryPopup);
        _bookshelfButton.onClick.AddListener(OpenReplayStoryPopup);
    }

    private void OnDisable()
    {
        if (_bookshelfButton == null)
        {
            return;
        }

        _bookshelfButton.onClick.RemoveListener(OpenReplayStoryPopup);
    }

    // 하우징 복귀 정보를 저장하고 다시보기 팝업 표시
    private void OpenReplayStoryPopup()
    {
        ResolveMissingReferences();

        if (_replayStoryPopup == null)
        {
            Debug.LogWarning("[HousingBookshelfReplayBinder] 스토리 다시보기 팝업을 찾지 못했습니다.", this);
            return;
        }

        _replayStoryPopup.OpenForHousingBookcase();
    }

    private void ResolveMissingReferences()
    {
        if (_bookshelfButton == null)
        {
            _bookshelfButton = GetComponent<Button>();
        }

        if (_replayStoryPopup != null)
        {
            return;
        }

        _replayStoryPopup = FindFirstObjectByType<ReplayStoryPopup>(FindObjectsInactive.Include);
    }
}
