// 담당자 : 김영찬
// 설명 : 인챈트 교체 팝업 View 인터페이스

// 1차 수정자 : 조규민
// 수정 내용 : 인챈트 교체 완료 확인 팝업 흐름을 위한 View 이벤트와 표시 메서드 추가

using System;
using System.Collections.Generic;

public interface IEnchantChangeView
{
    // 유저가 '이 인챈트를 버리겠다'고 최종 선택(확인)했을 때 발생 (매개변수: 버릴 Name_ID)
    event Action<int> OnDiscardConfirmed;

    // 교체 취소 확인 팝업에서 취소를 확정했을 때 발생
    event Action OnCancelClicked;

    // 교체 완료 확인 팝업에서 확인했을 때 발생
    event Action OnChangeCompleteConfirmed;

    // 새로 획득할 인챈트 정보를 화면에 세팅
    void SetNewEnchantInfo(EnchantDisplayData _newData);

    // 현재 보유 중인 인챈트 리스트(버릴 후보군)를 화면에 세팅
    void SetOwnedEnchantList(List<EnchantDisplayData> _ownedList);

    // 교체 완료 확인 팝업에 버릴 인챈트와 새 인챈트 정보를 표시
    void ShowChangeCompletePopup(EnchantDisplayData _discardData, EnchantDisplayData _newData);

    // EnchantListPresenter의 현재 리스트 데이터 받아옴
    EnchantListPresenter GetEnchantList();
}
