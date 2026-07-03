using UnityEngine;

public class VisualWrapperFitter : MonoBehaviour
{
    [Header("1. 대상 할당")]
    [Tooltip("애니메이터가 붙어있는 비주얼 객체 (예: prefab_0046_0050)")]
    public Transform visualTarget;
    [Tooltip("기준이 될 BoxCollider2D")]
    public BoxCollider2D targetCollider;

    [Header("2. 설정 (수정 후 우클릭으로 적용)")]
    [Tooltip("크기 축소 비율 (100 PPU 기준 0.005~0.01 추천)")]
    public float scaleFactor = 0.005f;
    
    [Tooltip("체크 시 콜라이더의 '정중앙'이 아닌 '바닥'에 발을 맞춥니다.")]
    public bool alignToBottom = true;

    [Tooltip("추가로 상하좌우 미세 조정이 필요할 때 사용")]
    public Vector2 manualOffset = Vector2.zero;

    [ContextMenu("💡 3. 여기에 우클릭하고 'Fit Now' 실행!")]
    public void FitNow()
    {
        if (visualTarget == null || targetCollider == null)
        {
            Debug.LogWarning("[오류] 비주얼 객체와 콜라이더를 드래그해서 할당해주세요!");
            return;
        }

        // 1. 애니메이터 간섭을 피하기 위한 껍데기(Wrapper) 찾기 또는 생성
        Transform wrapper;
        if (visualTarget.parent != transform)
        {
            wrapper = visualTarget.parent; // 이미 래퍼가 존재함
        }
        else
        {
            GameObject wrapperObj = new GameObject("Visual_Wrapper");
            wrapperObj.transform.SetParent(transform);
            wrapperObj.transform.localPosition = Vector3.zero;
            
            // 비주얼 객체를 껍데기 안으로 쏙 넣음
            visualTarget.SetParent(wrapperObj.transform);
            wrapper = wrapperObj.transform;
        }

        // 2. 껍데기의 크기를 축소 (애니메이터는 껍데기를 건드리지 못함!)
        wrapper.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

        // 3. 콜라이더 위치로 껍데기 이동
        Vector2 colOffset = targetCollider.offset;
        float targetY = alignToBottom 
            ? colOffset.y - (targetCollider.size.y / 2f) // 콜라이더 바닥
            : colOffset.y;                               // 콜라이더 중앙

        wrapper.localPosition = new Vector3(colOffset.x + manualOffset.x, targetY + manualOffset.y, 0f);

        Debug.Log($"[완료] 래퍼 정렬 끝! 크기가 안 맞으면 scaleFactor({scaleFactor})를 수정하고 다시 우클릭하세요.");
    }
}