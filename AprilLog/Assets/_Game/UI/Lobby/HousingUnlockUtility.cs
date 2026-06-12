//담당자: 조규민
using UnityEngine;

/// <summary>
/// 하우징 해금 조건 판정을 공통으로 담당한다.
/// </summary>
public static class HousingUnlockUtility
{
    public static bool IsChapterCleared(PlayerProgressModel _progressModel, int _chapter)
    {
        // 기능: 다음 챕터 첫 스테이지가 열렸는지를 기준으로 이전 챕터 클리어를 판단한다.
        if (_chapter <= 1)
            return true;

        if (_progressModel == null)
            return false;

        if (_progressModel.CurrentChapter > _chapter)
            return true;

        int _nextChapterFirstStageId = BuildStageId(_chapter + 1, 1);
        return _progressModel.IsStageUnlocked(_nextChapterFirstStageId);
    }

    public static int BuildStageId(int _chapter, int _stage)
    {
        // 기능: 챕터와 스테이지 번호를 프로젝트의 스테이지 ID 규칙으로 변환한다.
        return Mathf.Max(1, _chapter) * 100 + Mathf.Max(1, _stage);
    }
}
