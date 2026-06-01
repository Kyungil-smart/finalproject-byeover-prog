// 담당자 : 김영찬
// 설명   : 스킬 마스터 데이터 테이블
// 수정자 : 정승우
// 수정내용 : 테이블 클래스를 1클래스 1파일 구조로 정리

using UnityEngine;

[CreateAssetMenu(fileName = "Legacy_SkillMasterTable", menuName = "Data/Legacy_SkillMasterTable")]
public class Legacy_SkillMasterTable : DataTable<Legacy_SkillMasterData>
{
}
