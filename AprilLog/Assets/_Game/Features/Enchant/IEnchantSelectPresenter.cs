// 작성자 : 김영찬
// 설명: 통합 선택 로직과 분리 선택 로직 Presenter가 공통으로 가지고 있는 함수에 대한 인터페이스

using System;

public interface IEnchantSelectPresenter : IDisposable
{
    void ShowSelection(int pickCount = 3);
}