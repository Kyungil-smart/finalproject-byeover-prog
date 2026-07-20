using System;

[Serializable]
public class ReplayStoryData
{
    public string StoryId;
    public string ChapterTitle;
    public string EpisodeTitle;
    public ReplayStoryState State;
    public string UnlockConditionText;
    public string BackgroundResourcePath;

    public ReplayStoryData(
        string storyId,
        string chapterTitle,
        string episodeTitle,
        ReplayStoryState state,
        string unlockConditionText)
    {
        StoryId = storyId;
        ChapterTitle = chapterTitle;
        EpisodeTitle = episodeTitle;
        State = state;
        UnlockConditionText = unlockConditionText;
    }
}
