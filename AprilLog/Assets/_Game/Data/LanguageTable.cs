// 담당자 : 김영찬
// 설명   : 로컬라이제이션 데이터 테이블
// 수정자 : 정승우
// 수정내용 : 테이블 클래스를 1클래스 1파일 구조로 정리

using UnityEngine;

[CreateAssetMenu(fileName = "LanguageTable", menuName = "Data/LanguageTable")]
public class LanguageTable : DataTable<LanguageEntry>
{
}
