// 담당자 : 정승우
// 설명   : 튜토리얼 Presenter -- 단계 진행

public class TutorialPresenter
{
    private readonly ITutorialView _view;
    private int _currentStep;
    private int _totalSteps = 5;    // 나중에 데이터에서 가져오기

    public TutorialPresenter(ITutorialView view)
    {
        _view = view;
        _view.OnStepCompleted += HandleStepCompleted;
        _currentStep = 0;
    }

    public void Dispose()
    {
        _view.OnStepCompleted -= HandleStepCompleted;
    }

    public void StartTutorial()
    {
        _currentStep = 0;
        _view.ShowStep(_currentStep);
    }

    private void HandleStepCompleted()
    {
        _currentStep++;
        if (_currentStep >= _totalSteps)
        {
            _view.ClearHighlight();
            return;
        }
        _view.ShowStep(_currentStep);
    }
}
