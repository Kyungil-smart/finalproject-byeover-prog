// 담당자 : 김영찬
// 설명   : 캐릭터 전용 스탯 데이터 테이블
// 수정자 : Codex
// 수정내용 : 테이블 클래스를 1클래스 1파일 구조로 정리

using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStatusTable", menuName = "Data/CharacterStatusTable")]
public class CharacterStatusTable : DataTable<CharacterStatusData>
{
}
