// 담당자 : 김영찬
// 설명   : 캐릭터 마스터 데이터 테이블
// 수정자 : Codex
// 수정내용 : 테이블 클래스를 1클래스 1파일 구조로 정리

using UnityEngine;

[CreateAssetMenu(fileName = "CharacterMasterTable", menuName = "Data/CharacterMasterTable")]
public class CharacterMasterTable : DataTable<CharacterMasterData>
{
}
