//담당자: 조규민
// 부팅 중 세로 화면 고정과 Safe Area 재계산 완료까지의 안정화 대기

using System.Collections;
using UnityEngine;

// 앱 시작 직후 Portrait 방향과 화면 크기가 안정될 때까지 첫 UI 표시를 늦춘다.
public static class BootOrientationStabilizer
{
    private const int _requiredStableFrameCount = 3;
    private const int _maxWaitFrameCount = 90;

    // Portrait 화면 크기가 여러 프레임 동안 유지될 때까지 Boot UI 표시를 지연한다.
    public static IEnumerator WaitForPortraitStable()
    {
        int _stableFrameCount = 0;
        int _waitFrameCount = 0;
        int _lastScreenWidth = -1;
        int _lastScreenHeight = -1;

        while (_waitFrameCount < _maxWaitFrameCount && _stableFrameCount < _requiredStableFrameCount)
        {
            ApplyPortraitOrientation();

            int _screenWidth = Screen.width;
            int _screenHeight = Screen.height;
            bool _hasValidScreenSize = _screenWidth > 0 && _screenHeight > 0;
            bool _isPortrait = _hasValidScreenSize && _screenHeight >= _screenWidth;
            bool _isSameSize = _screenWidth == _lastScreenWidth && _screenHeight == _lastScreenHeight;

            _stableFrameCount = _isPortrait && _isSameSize ? _stableFrameCount + 1 : 0;
            _lastScreenWidth = _screenWidth;
            _lastScreenHeight = _screenHeight;
            _waitFrameCount++;

            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        RefreshActiveSafeAreas();
        Canvas.ForceUpdateCanvases();
        yield return null;

        RefreshActiveSafeAreas();
        Canvas.ForceUpdateCanvases();
    }

    // Android 초기 프레임에서도 Portrait 방향만 허용되도록 Unity 화면 방향 값을 반복 적용한다.
    private static void ApplyPortraitOrientation()
    {
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.Portrait;
    }

    // 방향 안정화 이후 활성화된 SafeAreaFitter를 다시 계산해 첫 UI 배치 흔들림을 줄인다.
    private static void RefreshActiveSafeAreas()
    {
        var _safeAreaFitters = Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int _index = 0; _index < _safeAreaFitters.Length; _index++)
        {
            _safeAreaFitters[_index].Refresh(true);
        }
    }
}
