using System;
using UnityEngine;

/// <summary>
/// MonsterSpawner의 스폰 바운더리를 유니티 에디터 씬(Scene) 뷰에 시각화해 주는 전용 컴포넌트
/// </summary>
[RequireComponent(typeof(MonsterSpawner))]
public class MonsterSpawnerGizmo : MonoBehaviour
{
    [SerializeField] private bool _isGizmoActive;
    
    private MonsterSpawner _spawner;

    private void OnDrawGizmos()
    {
        if (!_isGizmoActive) return;
        
        if (_spawner == null)
        {
            _spawner = GetComponent<MonsterSpawner>();
            if (_spawner == null) return;
        }
        
        float y = _spawner.NormalSpawnLineY;
        float minX = _spawner.NormalSpawnLineXMin;
        float maxX = _spawner.NormalSpawnLineXMax;
        Transform[] points = _spawner.SpawnPoints;
        
        // 고정 스폰 포인트 (크기 1의 노란색 원 -> 반지름 0.5)
        Gizmos.color = Color.yellow;
        if (points != null)
        {
            foreach (var point in points)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                }
            }
        }
        
        // 소환 라인 (높이 1의 붉은색 사각형)
        float width = maxX - minX;
        float centerX = minX + (width / 2f);

        Vector3 boxCenter = new Vector3(centerX, y, 0f);
        Vector3 boxSize = new Vector3(width, 1f, 0.1f); // 2D 환경이므로 두께는 얇게
        
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); 
        Gizmos.DrawCube(boxCenter, boxSize);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCenter, boxSize);
        
        // X 최소/최대 바운더리 라인 (상단 모서리에서 아래로 길이 5)
        float topY = y + 0.5f;
        float bottomY = topY - 5f;
        
        Vector3 topLeft = new Vector3(minX, topY, 0f);
        Vector3 bottomLeft = new Vector3(minX, bottomY, 0f);
        
        Vector3 topRight = new Vector3(maxX, topY, 0f);
        Vector3 bottomRight = new Vector3(maxX, bottomY, 0f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(topLeft, bottomLeft);
        Gizmos.DrawLine(topRight, bottomRight);
    }
}
