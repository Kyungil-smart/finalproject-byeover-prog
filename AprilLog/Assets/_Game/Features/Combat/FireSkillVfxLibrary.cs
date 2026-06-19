// 담당자 : 정승우
// 설명   : 불 속성 스킬 VFX 프리팹 모음 (Fire_Asset 패키지 연결용 ScriptableObject)
//          Resources/FireSkillVfxLibrary.asset 으로 두고 SkillSystem이 Resources.Load로 읽는다.
//          -> 씬마다 손으로 연결할 필요 없음. 스케일/타이밍은 인스펙터에서 조정.

using UnityEngine;

[CreateAssetMenu(fileName = "FireSkillVfxLibrary", menuName = "AprilLog/Fire Skill VFX Library")]
public class FireSkillVfxLibrary : ScriptableObject
{
    [Header("메테오 (3단: 생성 구역 -> 몸체 낙하 -> 착탄 폭발)")]
    [Tooltip("생성 구역 마커 (Flame_ellipse)")]
    public GameObject meteorMarker;
    [Tooltip("낙하하는 몸체 (Fireball_loop_2)")]
    public GameObject meteorBall;
    [Tooltip("착탄 폭발 (explosion_5)")]
    public GameObject meteorExplosion;

    public float meteorMarkerScale = 1f;
    public float meteorBallScale = 1f;
    public float meteorExplosionScale = 1f;

    [Tooltip("생성 구역(ellipse) 표시 후 낙하 시작까지 (예고 시간)")]
    public float meteorTelegraph = 0.5f;
    [Tooltip("몸체 낙하 시간")]
    public float meteorFallTime = 0.35f;
    [Tooltip("생성 구역이 떠 있는 높이 (착탄 지점 기준 위쪽, 월드 단위)")]
    public float meteorFallHeight = 3.5f;

    [Tooltip("생성 구역의 가로 치우침(월드 단위). 양수 = 착탄 지점보다 오른쪽 위에 생성되어 '왼쪽 사선'으로 낙하")]
    public float meteorFallOffsetX = 1.2f;

    [Tooltip("몸체 회전 미세조정(도). 기본 회전은 낙하 방향에서 자동 계산되고, 이 값은 거기에 더해진다")]
    public float meteorBallRotationTrimDeg = 0f;

    [Tooltip("몸체 소멸 후 폭발까지 한 박자(초). 폭발이 몸체를 가리는 걸 방지")]
    public float meteorExplosionDelay = 0.05f;

    [Tooltip("착탄 폭발(explosion_5)을 착탄 지점에서 위로 올리는 세로 오프셋(px). 기획 4-4-1 explosion Position y=130. " +
             "범위가 커지면 이 오프셋도 폭발 크기와 같은 비율로 함께 커진다(기획 4-4-2: 1.1배 → 130→143px)")]
    public float meteorExplosionOffsetYPx = 130f;

    [Tooltip("켜면 착탄 시 실제 공격 범위(반투명 사각형)를 함께 표시 — 폭발 크기를 공격 범위에 맞출 때 사용 후 끌 것")]
    public bool debugShowHitArea = true;

    [Header("화염 작렬 (연결됨) — Fireball_2_normal")]
    public GameObject fireballProjectile;   // Fireball_2_normal
    [Tooltip("화염 작렬 투사체 VFX 스케일 보정")]
    public float fireballProjectileScale = 1f;
    [Tooltip("투사체 방향 회전 미세조정(도). 기본 회전은 진행 방향으로 자동 계산되고 이 값이 더해진다 (방향이 뒤집히면 180 넣어 보정)")]
    public float fireballRotationTrimDeg = 0f;

    [Header("파이어브레스 (연결됨) — Magic_Sphere_normal / Flame_normal")]
    public GameObject fireBreathCrystal;    // Magic_Sphere_normal (수정구)
    [Tooltip("수정구(구체) 스케일 — 단순 소환 비주얼이라 '범위 스탯'과 무관한 고정 크기. 너무 크면 더 줄일 것 (실제 공격은 브레스 화염이 함)")]
    public float fireBreathCrystalScale = 0.14f;
    public GameObject fireBreathFlame;      // Flame_normal (화염 분사)
    [Tooltip("화염(브레스) 기준 스케일 — 실제 크기 = 이 값 × 범위배율(피격범위폭/500px). 범위 스탯↑ 시 화염도 비례 확대")]
    public float fireBreathFlameScale = 0.4f;
    [Tooltip("화염 분사 간격(초) — 기획 4-1-4 '0.5초 간격마다'")]
    public float fireBreathFlameInterval = 0.5f;
    [Tooltip("수정구에서 적 방향으로 화염을 띄우는 거리(px) — 기획 4-1 Flame Position y=400. PxToWorld 변환")]
    public float fireBreathFlameYOffsetPx = 400f;
    [Tooltip("화염 방향 회전 미세조정(도). 기본은 적 방향 자동계산(+X 제작 에셋), 뒤집히면 180/±90")]
    public float fireBreathFlameRotationTrimDeg = 0f;

    [Header("대지 균열 (연결됨) — Ground_explosion_normal")]
    public GameObject earthCrackExplosion;  // Ground_explosion_normal (크랙 폭발)
    [Tooltip("크랙 1개 스케일 보정 — Ground_explosion 파티클 native size가 ~10월드유닛이라 1이면 화면을 덮음. 0.2≈2유닛(서로 거의 맞닿는 크랙 라인). 인스펙터에서 튜닝 (기획 4-3 Scale 45x50)")]
    public float earthCrackScale = 0.2f;
    public GameObject earthCrackEmber;      // fire_big (잔불, 옵션 — 현재 미사용)

    [Header("추후 연결 — 정령")]
    public GameObject spiritAttack;         // Uzi_red (용도 확인 중)
    public GameObject[] hitEffects;         // expl_01_01~05
}
