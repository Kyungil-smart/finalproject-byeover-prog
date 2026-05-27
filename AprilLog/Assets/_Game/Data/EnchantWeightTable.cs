// 담당자 : 김영찬
// 설명   : 인챈트 가중치 데이터 테이블
// 수정자 : Codex
// 수정내용 : 테이블 클래스를 1클래스 1파일 구조로 정리

using UnityEngine;

[CreateAssetMenu(fileName = "EnchantWeightTable", menuName = "Data/EnchantWeightTable")]
public class EnchantWeightTable : DataTable<EnchantWeightData>
{
}
