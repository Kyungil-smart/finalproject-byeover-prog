// 담당자 : 김영찬
// 설명   : ScriptableObject 데이터 테이블 베이스 클래스
// 수정자 : 정승우
// 수정내용 : 테이블 클래스를 1클래스 1파일 구조로 정리

using System.Collections.Generic;
using UnityEngine;

public abstract class DataTable<T> : ScriptableObject
{
    [Header("데이터")]
    public List<T> rows = new List<T>();
}
