// 담당자 : 김영찬
// 설명 : 인챈트 교체 팝업 View 인터페이스

using System;
using System.Collections.Generic;

public interface IEnchantChangeView
{
    // 유저가 '이 인챈트를 버리겠다'고 최종 선택(확인)했을 때 발생 (매개변수: 버릴 Name_ID)
    event Action<int> OnDiscardConfirmed;
    
    // 교체를 취소하고 창을 닫을 때 발생
    event Action OnCancelClicked;

    // 새로 획득할 인챈트 정보를 화면에 세팅
    void SetNewEnchantInfo(EnchantDisplayData newData);

    // 현재 보유 중인 인챈트 리스트(버릴 후보군)를 화면에 세팅
    void SetOwnedEnchantList(List<EnchantDisplayData> ownedList);
    
    // EnchantListPresenter의 현재 리스트 데이터 받아옴
    EnchantListPresenter GetEnchantList();
}