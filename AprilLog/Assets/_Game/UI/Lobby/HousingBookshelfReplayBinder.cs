//담당자: 조규민

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 책장에서 스토리 다시보기 팝업을 엽니다.
/// </summary>
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

        _replayStoryPopup = FindSceneComponentByName<ReplayStoryPopup>("POPUp_RePlayStory");

        if (_replayStoryPopup == null)
        {
            _replayStoryPopup = FindSceneComponentByName<ReplayStoryPopup>("POPUP_RePlayStory");
        }
    }

    private static T FindSceneComponentByName<T>(string _objectName) where T : Component
    {
        Transform[] _transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int _index = 0; _index < _transforms.Length; _index++)
        {
            Transform _target = _transforms[_index];

            if (_target == null || _target.name != _objectName)
            {
                continue;
            }

            GameObject _gameObject = _target.gameObject;

            if (_gameObject.scene.IsValid() == false)
            {
                continue;
            }

            return _gameObject.GetComponent<T>();
        }

        return null;
    }
}
