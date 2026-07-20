// 작성자 : 홍정옥
// 인게임 다이제틱 대화 한 줄. 데이터 소스와 무관하게 프리젠터에 넘기는 최소 단위.

public enum TalkSpeaker
{
    April = 0,
    Rary = 1,
}

public readonly struct TalkLine
{
    public readonly TalkSpeaker Speaker;
    public readonly string Name;
    public readonly string Text;

    public TalkLine(TalkSpeaker speaker, string name, string text)
    {
        Speaker = speaker;
        Name = name;
        Text = text;
    }
}
