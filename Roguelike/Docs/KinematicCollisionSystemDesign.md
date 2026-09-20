# 2D 키네마틱 이동 및 충돌 시스템 설계

## 1. 문서 상태

- 대상: Unity 6 기반 2D 탑다운 로그라이크
- 단계: 구현 전 설계 초안
- 1차 목표: `Rigidbody2D` 없이 이동, 이산 충돌 판정, 침투 해소를 직접 처리한다.
- 공간 분할: 정적 충돌체에만 Quadtree를 사용한다.

## 2. 결정 요약

1. `Rigidbody2D`와 Unity 물리 응답은 사용하지 않는다.
2. Unity `Collider2D`와 `Physics2D`는 사용하지 않고 충돌 도형 데이터를 직접 정의한다.
3. 위치, 프레임 이동량, 충돌 판정, 충돌 해소는 커스텀 월드가 소유한다.
4. 첫 버전은 sweep/CCD 없이 이동 후 겹침을 검사하는 이산 충돌만 지원한다.
5. 첫 버전의 좁은 단계 조합은 Circle-Circle과 Circle-AABB이다.
6. 정적 충돌체는 Quadtree에 등록하고 동적 충돌체는 1차 구현에서 브루트포스로 후보 쌍을 만든다.
7. 각 Pawn이 개별적으로 움직이고 즉시 충돌을 푸는 방식이 아니라, 중앙 `KinematicWorld`가 모든 바디를 한 번에 갱신한다.

> `circle-box`와 `box-circle`은 별개의 충돌 알고리즘이 아니다. 같은 충돌쌍에서 입력 순서와 법선 방향만 반대다. 하나의 Circle-AABB 판정 함수로 정규화한다.

## 3. 범위

### 포함

- 속도 기반 이동과 즉시 위치 변경(텔레포트)
- 동적 바디와 정적 바디
- 직접 정의한 Circle과 회전하지 않은 AABB 도형
- Circle-Circle 및 Circle-AABB 겹침 판정
- 최소 이동 벡터(MTV)를 이용한 위치 보정
- MTV 보정으로 접선 이동을 유지하는 기본 슬라이딩
- 레이어/마스크 기반 충돌 필터링
- 정적 충돌체의 Quadtree 등록 및 영역 질의
- 동적 충돌체 목록의 브루트포스 후보 쌍 생성
- 충돌 시작/유지/종료 이벤트를 나중에 추가할 수 있는 접촉 결과 구조

### 제외

- sweep, shape cast, TOI(Time of Impact), 연속 충돌 검출
- Box-Box 충돌
- 회전된 Box(OBB)
- 반발력, 마찰, 질량 기반 물리 충격량
- 각속도와 회전 충돌
- 조인트, 중력, Unity 물리 이벤트
- 완전한 결정론 및 네트워크 롤백

Box-Box가 등록되면 조용히 통과시키지 않고 개발 빌드에서 경고를 남긴다. 지원 조합이 필요해지는 시점에 좁은 단계 디스패처만 확장한다.

## 4. 커스텀 충돌 도형 사용 방침

Unity `Collider2D`, `Rigidbody2D`, `Physics2D`는 런타임 충돌 시스템에 사용하지 않는다. 충돌 범위는 직렬화 가능한 커스텀 데이터로 직접 정의하고, 판정과 해소도 커스텀 코드만 수행한다.

```text
CollisionShape2D
├── ShapeType: Circle 또는 Aabb
├── Offset
└── Extents
    ├── Circle: (radius, radius)
    └── Aabb:   (halfWidth, halfHeight)
```

Scene과 Prefab에서 범위를 편집해야 하는 객체에는 얇은 `KinematicBodyAuthoring` MonoBehaviour를 사용한다. 이 컴포넌트는 충돌을 처리하지 않고 다음 역할만 가진다.

- Shape, Static/Dynamic, Layer/Mask 직렬화
- 런타임 바디 등록에 필요한 작성 데이터 제공
- `OnDrawGizmosSelected`에서 Circle 또는 AABB 와이어 표시
- 잘못된 반지름, 크기, 회전에 대한 `OnValidate` 검증

런타임 `KinematicBody2D`, 충돌 판정, Solver와 Quadtree는 순수 C#로 구현한다.

### 도형 제약

- Box는 월드 축에 정렬된 AABB만 허용한다. Z 회전이 0이 아닌 경우 등록을 거부하거나 경고한다.
- Circle은 원형을 유지해야 하므로 X/Y 스케일을 동일하게 사용한다. 비균일 스케일은 경고한다.
- 음수 스케일은 절댓값으로 크기를 계산하되, 제작 단계에서 사용하지 않는 것을 원칙으로 한다.
- 충돌 중심은 바디 위치와 로컬 `Offset`으로 계산한다.
- `Extents`의 각 축은 0보다 커야 한다. Circle은 X와 Y가 같은 값이어야 한다.

## 5. 좌표 및 데이터 모델

### KinematicBody2D

MonoBehaviour가 아닌 순수 C# 시뮬레이션 객체다.

- 고유하고 재사용되지 않는 `BodyId`
- 현재 위치와 이전 위치
- 요청받은 프레임 이동량(`moveDelta`)과 해소 후 실제 이동량
- `Static` 또는 `Dynamic` 이동 타입
- 역질량: 정적 바디는 0, 일반 동적 바디는 기본 1
- 충돌 레이어와 충돌 마스크
- 텔레포트 요청
- 연결된 `CollisionShape2D` 데이터

### CollisionShape2D

두 도형을 별도 상속 계층으로 만들지 않고, `ShapeType` 태그를 가진 하나의 직렬화 가능한 구조체로 표현한다.

- `ShapeType.Circle`: `Extents = (radius, radius)`로 저장하고 `Radius` 프로퍼티는 `Extents.x`를 반환한다.
- `ShapeType.Aabb`: `Extents`를 중심부터 각 면까지의 거리인 half extents로 사용한다.
- 도형 조합은 `(shapeA.Type, shapeB.Type)`으로 디스패치한다.
- AABB-Circle은 Circle-AABB로 순서를 바꿔 검사한 뒤 접촉 법선을 반전한다.
- 정적 Quadtree에 필요한 경계는 Unity `Rect`로 계산하여 등록 시 캐시한다.
- 동적 도형의 월드 중심과 경계는 PostUpdate/PreLateUpdate 시점에 한 번 계산해 해당 스텝 안에서 재사용한다.

`Radius`와 `HalfExtents`를 별도 필드로 동시에 보관하지 않는다. 잘못된 Extents를 만들지 않도록 `CreateCircle`과 `CreateAabb` 팩터리 메서드로 생성한다. 인스펙터에서는 커스텀 PropertyDrawer가 Circle일 때 단일 `Radius`, Aabb일 때 `Half Extents`로 표시한다.

### 작성 데이터와 등록

- `PawnCreator`로 만드는 동적 객체는 Definition 또는 Authoring의 `CollisionShape2D`를 복사해 `KinematicWorld`에 직접 등록한다.
- 씬에 배치된 정적 벽은 `KinematicBodyAuthoring`으로 작성하고 Stage 또는 Bootstrap이 초기화할 때 수집해 등록한다.
- 정적 Shape의 크기, 위치 또는 스케일이 런타임에 바뀌면 Quadtree의 `Rect` 캐시를 다시 계산한다.
- 런타임 시뮬레이션 데이터는 Authoring 컴포넌트를 직접 수정하지 않고 `KinematicBody2D`가 소유한다.

### KinematicWorld

순수 C# 객체로 구현하며 다음을 소유한다.

- 바디 등록부
- 정적 충돌체 Quadtree
- 동적 충돌체 목록
- 후보 충돌쌍 버퍼
- 접촉 정보 버퍼
- 프레임 이동량 적용과 반복 충돌 해소

월드 실행은 기존 Stage 또는 Bootstrap의 Unity 생명주기 진입점에서 호출한다.

## 6. 프레임 처리 순서

모든 바디를 같은 월드 단계에서 처리해야 오브젝트 Update 호출 순서에 따른 결과 차이를 줄일 수 있다.

1. 각 Pawn과 AI의 `Update`가 `velocity * dt`를 계산해 프레임 이동량으로 `MoveRequest`에 담는다.
2. 모든 `Update`가 끝난 뒤, `LateUpdate`가 시작되기 전인 PostUpdate/PreLateUpdate 시점에 중앙 실행기가 `KinematicWorld.Step()`을 한 번 호출한다.
3. 월드는 `현재 위치 + moveDelta`로 모든 동적 바디의 예상 위치를 계산한다. 이때 아직 Transform에는 반영하지 않는다.
4. 텔레포트 요청을 적용한다. 텔레포트도 목적지에서 겹침 해소를 수행하는 것을 기본값으로 한다.
5. 예상 위치를 기준으로 광역 단계 후보를 질의한다.
6. 레이어, 자기 자신, 타입 조합을 필터링하고 중복 쌍을 제거한다.
7. 좁은 단계에서 접촉 법선과 침투 깊이를 구한다.
8. MTV를 예상 위치에 적용해 침투를 해소한다.
9. 여러 벽 또는 여러 바디 접촉을 위해 제한된 횟수만큼 5~8단계를 반복한다.
10. 해소가 끝난 최종 위치를 Transform에 한 번 반영한다.

첫 구현의 시뮬레이션 주기는 렌더 프레임과 같은 가변 시간 간격을 사용한다. 이동 요청은 `Update`에서 만들고 충돌 판정과 해소는 PostUpdate/PreLateUpdate 시점에 한 번 수행한다. 큰 프레임 지연은 이산 충돌의 터널링 가능성을 높이므로, 이후 문제가 확인되면 `dt` 상한 또는 sub-step을 추가한다.

`Update`에서 각 Transform을 먼저 옮긴 다음 나중에 고치는 방식은 사용하지 않는다. 다른 컴포넌트의 `Update`가 일부만 이동한 중간 상태를 볼 수 있기 때문이다. Update에서는 커스텀 바디의 요청만 갱신하고, Transform 반영은 중앙 월드의 PostUpdate/PreLateUpdate 실행으로 제한한다. 이후 `LateUpdate`를 사용하는 카메라와 시각 시스템은 해소가 끝난 최종 위치를 읽는다.

## 7. 이동 규칙

`MoveRequest.Vector`는 속도가 아니라 이번 시뮬레이션 스텝에 적용할 이동량으로 정의한다. 단위는 `world unit / step`이다.

- `Active`: 한 스텝에서 마지막 요청을 사용한다.
- `Passive`: 모든 요청을 누적한다. 감쇠 및 Active와의 우선순위는 별도 이동 정책으로 둔다.
- `Teleport`: 마지막 요청이 우선하며 해당 스텝의 일반 이동을 대체한다.

기존 `PawnMovement`는 `Rigidbody2D.linearVelocity`와 `MovePosition` 대신 `KinematicBody2D`에 `moveDelta` 또는 텔레포트를 기록한다. 실제 위치 변경은 `KinematicWorld`만 수행한다.

요청을 만드는 쪽에서 속도를 프레임 이동량으로 변환한다.

```text
moveDelta = velocity * dt
```

키네마틱 컨트롤러는 `dt`를 다시 곱하지 않고 이동량을 위치에 더한다.

```text
predictedPosition = currentPosition + moveDelta
```

따라서 컨트롤러 내부에는 별도의 물리 적분 단계가 없다. `velocity * dt` 계산은 이미 요청 생성 측에서 끝난 것으로 취급한다. 충돌 해소 후 실제 이동량이 필요하면 `resolvedPosition - previousPosition`으로 구한다.

## 8. 좁은 단계 충돌 판정

### Circle-Circle

두 원의 중심을 `CA`, `CB`, 반지름을 `rA`, `rB`라고 한다.

1. `delta = CA - CB`를 계산한다.
2. `radiusSum = rA + rB`를 계산한다.
3. `dot(delta, delta) < radiusSum * radiusSum`이면 겹친다.
4. `normal = normalize(delta)`는 B에서 A를 밀어낼 방향이다.
5. `depth = radiusSum - length(delta)`다.
6. A에 적용할 탈출 MTV는 `normal * depth`다.

두 원의 중심이 정확히 같아 법선을 만들 수 없다면 이전 프레임의 상대 위치를 우선 사용한다. 그것도 없으면 BodyId 순서로 고정된 축을 선택해 실행할 때마다 방향이 바뀌지 않게 한다.

### Circle-AABB

입력은 원 중심 `C`, 반지름 `r`, 박스 최소점 `Bmin`, 최대점 `Bmax`다.

#### 원 중심이 박스 바깥에 있는 일반 경우

1. `C`의 각 축을 `[Bmin, Bmax]`에 clamp하여 박스 위의 최근접점 `P`를 구한다.
2. `delta = C - P`를 계산한다.
3. `dot(delta, delta) < r * r`이면 겹친다.
4. `normal = normalize(delta)`는 박스에서 원을 밀어낼 방향이다.
5. `depth = r - length(delta)`다.
6. 원에 적용할 MTV는 `normal * depth`다.

제곱 거리 비교를 먼저 사용해 충돌하지 않는 대부분의 경우 제곱근 계산을 피한다.

#### 원 중심이 박스 내부에 있는 경우

최근접점과 원 중심이 같아 일반 식으로는 법선을 만들 수 없다.

1. 원 중심에서 박스의 네 면까지 거리를 구한다.
2. 가장 가까운 면의 바깥 방향을 법선으로 선택한다.
3. `depth = 선택한 면까지의 거리 + r`로 계산한다.
4. 이전 위치 또는 이동 방향은 거리가 같은 경우의 타이브레이커로만 사용한다.

#### 입력 순서 정규화

- Circle-Box 입력은 그대로 처리한다.
- Box-Circle 입력은 내부적으로 Circle-Box로 순서를 바꿔 처리한 뒤 접촉 법선을 반전한다.
- 쌍의 저장 순서는 `BodyId`가 작은 바디를 A로 고정해 중복을 방지한다.

접촉 결과는 최소한 다음 값을 가진다.

```text
Contact {
    bodyA,
    bodyB,
    separationNormalForA,
    penetrationDepth,
    contactPoint
}
```

## 9. 충돌 해소

### 위치 보정

- Dynamic-Static: 동적 바디에 MTV 전체를 적용한다.
- Static-Dynamic: 위와 동일하되 법선 방향만 정규화한다.
- Dynamic-Dynamic: 두 바디의 역질량 비율로 MTV를 나눈다.
- Static-Static: 판정 및 해소 대상에서 제외한다.

1차 구현에서는 `slop`을 사용하지 않고 `depth > 0`인 침투를 전체 깊이만큼 보정한다. 실제 테스트에서 부동소수점 오차로 인한 미세 떨림이 확인될 때만 작은 허용 오차를 추가한다.

### 이동 결과와 슬라이딩

이산 방식에서는 요청받은 전체 `moveDelta`의 법선 성분을 제거하지 않는다. 시작 위치와 벽 사이에 있던 정상적인 이동 거리까지 취소될 수 있기 때문이다.

접촉 법선 `n`을 충돌체 밖으로 나가는 방향, 침투 깊이를 `depth`라고 정의하면 탈출 MTV는 `n * depth`다. 이 벡터는 예상 위치에 더한다.

```text
predictedPosition = currentPosition + requestedMoveDelta
separationMtv = outwardNormal * penetrationDepth
resolvedPosition = predictedPosition + separationMtv
actualMoveDelta = resolvedPosition - currentPosition
```

예를 들어 오른쪽으로 6만큼 이동했지만 벽에 1만큼 침투했다면 왼쪽을 향하는 MTV 1만 적용하여 실제로는 오른쪽으로 5만큼 이동한다. 대각선 이동에서는 MTV가 벽의 수직 성분만 보정하고 접선 성분은 그대로 남기 때문에 기본적인 슬라이딩이 만들어진다.

MTV를 침투 방향으로 정의하는 구현이라면 예상 위치에서 빼야 하지만, 부호 혼동을 피하기 위해 이 문서에서는 항상 "바디를 충돌체 밖으로 꺼내는 탈출 벡터"로 정의하고 위치에 더한다. 첫 버전에는 반발 계수와 마찰을 넣지 않는다.

### 반복 해소

한 충돌쌍은 MTV를 한 번 적용하면 완전히 분리된다. 다만 그 보정이 다른 충돌쌍의 침투를 만들거나, 여러 동적 바디 사이의 보정이 연쇄적으로 전달될 수 있으므로 전체 후보 쌍 해소를 제한된 횟수만큼 다시 검사한다. 기본 최대값은 4패스로 시작하고 실제 장면을 프로파일링해 조절한다. 4패스를 항상 실행하는 것은 아니며, 침투가 하나도 없으면 즉시 종료한다.

- 각 반복에서 동적-정적 후보를 다시 질의하고 동적-동적 고유 쌍을 다시 검사한다.
- 더 이상 침투가 없으면 조기 종료한다.
- 최대 반복 후에도 침투가 남으면 개발 빌드에서 BodyId, 침투 깊이, 위치를 기록한다.
- 후보와 접촉을 BodyId 기준으로 안정 정렬해 실행 순서 변화에 따른 흔들림을 줄인다.

## 10. Quadtree 광역 단계

Octree는 사용하지 않는다. XY 평면만 사용하는 현재 게임에서는 정적 충돌체의 광역 단계에만 Quadtree를 사용한다.

### 노드 정책

- 각 노드는 AABB 영역, 아이템 목록, 최대 4개의 자식을 가진다.
- `maxItemsPerNode`를 넘고 `maxDepth` 미만이면 분할한다.
- 아이템 AABB가 한 자식에 완전히 들어갈 때만 자식으로 내린다.
- 경계에 걸친 아이템은 현재 부모 노드에 한 번만 보관한다.

이 정책은 하나의 충돌체가 여러 노드에 중복 저장되는 문제를 피한다.

### 정적 Quadtree와 동적 브루트포스

- 정적 Quadtree: 벽처럼 움직이지 않는 바디를 보관하며 맵 또는 정적 충돌체가 바뀔 때만 재구축한다.
- 동적 충돌체는 Quadtree에 넣지 않고 별도의 연속된 목록에 보관한다.
- Dynamic-Static 후보는 각 동적 바디의 AABB로 정적 Quadtree를 질의해 만든다.
- Dynamic-Dynamic 후보는 동적 목록의 인덱스 `i`, `j`에 대해 `j = i + 1`부터 순회하여 각 고유 쌍을 한 번만 만든다.
- Static-Static 쌍은 검사하지 않는다.
- 레이어, 마스크와 지원 도형 조합 필터는 좁은 단계 전에 적용한다.

1차 구현에서는 동적 오브젝트 수가 적고 Circle 계열 판정 비용이 낮다고 가정하여 브루트포스의 단순성과 검증 용이성을 우선한다. 매 프레임 다음 수치를 프로파일링할 수 있게 계측 지점을 둔다.

- 동적 바디 수
- 동적-동적 전체 후보 쌍 수
- 필터를 통과한 좁은 단계 호출 수
- 실제 접촉 수
- 동적 충돌 검사 소요 시간

측정 결과 동적 브루트포스가 병목일 때만 동적 Quadtree 또는 Spatial Hash Grid를 도입한다. 크기가 비슷한 이동 객체가 많은 탑다운 환경에서는 매 프레임 비우고 다시 채우는 Spatial Hash Grid를 우선 비교 대상으로 둔다.

### 월드 경계

루트 영역은 스테이지의 플레이 가능 영역을 포함하도록 명시적으로 설정한다. 범위를 벗어난 바디는 누락시키지 말고 다음 중 하나로 처리한다.

- 개발 빌드에서는 오류를 기록한다.
- 런타임에서는 루트 영역을 확장하고 트리를 재구축한다.

첫 버전은 고정된 스테이지 경계를 사용하고 범위 이탈을 오류로 취급하는 쪽이 단순하다.

## 11. 충돌 필터

좁은 단계 전에 다음 순서로 필터링한다.

1. 자기 자신인지 확인
2. 두 바디가 모두 Static인지 확인
3. 양쪽 Layer/Mask가 서로 허용하는지 확인
4. 지원하는 도형 조합인지 확인

Trigger는 겹침만 감지하고 위치를 보정하지 않는 별도 기능이므로 1차 구현 범위에서 제외한다. 아이템 획득 범위나 공격 판정에 필요해지는 시점에 이동 충돌과 분리된 Overlap 시스템으로 설계한다.

## 12. 이산 충돌의 한계와 안전장치

이산 충돌은 한 스텝의 시작과 끝 사이에 얇은 벽을 완전히 통과하면 충돌을 발견하지 못한다. 이는 구현 오류가 아니라 현재 범위의 명시적인 한계다.

첫 버전의 안전장치는 다음과 같다.

- 비정상적으로 큰 `dt`를 제한한다.
- `moveDelta`가 최소 장애물 두께보다 큰 프레임을 계측하여 터널링 위험을 확인한다.
- 실제 문제가 확인되면 총이동량을 줄이지 않고 한 프레임 이동을 여러 sub-step으로 나눈다.
- 아주 빠른 투사체는 커스텀 키네마틱 바디 대상에서 제외하거나 별도 처리한다.

이후 sweep을 추가할 때 Quadtree 질의 영역을 시작 AABB와 끝 AABB의 합집합인 swept AABB로 확장하고, 좁은 단계에 Circle-vs-AABB TOI를 추가한다. 현재 바디/도형/광역 단계 구조는 그대로 재사용한다.

## 13. 성능 원칙

- 스텝 중 `new`, LINQ, boxing을 피한다.
- 후보, 접촉, 쿼리 결과 컬렉션은 재사용한다.
- 거리 비교에는 가능한 한 제곱 거리를 사용한다.
- Transform 접근은 월드 도형 갱신과 최종 위치 반영 시점으로 제한한다.
- 정적 도형의 월드 데이터와 AABB는 변경 전까지 캐시한다.
- 최적화 여부는 바디 수, 후보 쌍 수, 실제 접촉 수, 트리 재구축 시간을 측정한 뒤 결정한다.

## 14. 권장 폴더 및 네임스페이스

프로젝트의 폴더-네임스페이스 규칙에 맞춰 별도 물리 모듈을 둔다.

```text
Assets/Scripts/KinematicPhysics2D/
├── Data/
│   ├── ShapeType.cs
│   ├── CollisionShape2D.cs
│   └── Contact.cs
├── Interface/
│   ├── IKinematicBody.cs
│   └── IKinematicWorld.cs
├── Runtime/
│   ├── KinematicWorld.cs
│   ├── KinematicBody2D.cs
│   ├── KinematicBodyAuthoring.cs
│   ├── Collision/
│   │   ├── CollisionDispatcher.cs
│   │   ├── CircleCircleCollision.cs
│   │   ├── CircleAabbCollision.cs
│   └── Spatial/
│       └── Quadtree.cs
└── Tests/
    ├── CircleCircleCollisionTests.cs
    ├── CircleAabbCollisionTests.cs
    ├── CollisionSolverTests.cs
    └── QuadtreeTests.cs
```

예상 네임스페이스는 `KinematicPhysics2D.Data`, `KinematicPhysics2D.Interface`, `KinematicPhysics2D.Runtime`, `KinematicPhysics2D.Runtime.Collision`, `KinematicPhysics2D.Runtime.Spatial`이다.

## 15. 현재 코드에서의 변경 지점

- `PawnCreator`: `Rigidbody2D` 생성 코드를 제거하고 Definition의 `CollisionShape2D`로 Kinematic Body를 생성해 월드에 직접 등록한다.
- `PawnMovement`: 생성자 의존성을 `Rigidbody2D`에서 Kinematic Body의 이동 명령 인터페이스로 바꾼다.
- `PawnMovement.OnMove`: `velocity * dt`로 계산한 `moveDelta`와 텔레포트 요청을 바디에 기록만 한다.
- `Pawn.Update`: 월드의 실제 위치 갱신을 직접 호출하지 않는다.
- Bootstrap/Stage: `KinematicWorld` 생성, 스테이지 경계 설정, PostUpdate/PreLateUpdate 시점의 월드 실행과 Dispose를 담당한다.

`KinematicWorld.Step`은 모든 Pawn이 이동 요청을 제출한 뒤 한 번만 호출되어야 한다. 현재처럼 각 Pawn의 Update 안에서 곧바로 이동까지 완료하면 등록 순서에 따라 결과가 달라질 수 있다.

## 16. 검증 계획

### 단위 테스트

- 원이 박스의 상하좌우 및 네 모서리와 겹치는 경우
- 접하기만 하는 경우와 아주 작은 침투가 있는 경우
- 원 중심이 박스 내부, 중심, 면과 같은 위치에 있는 경우
- Circle-Box와 Box-Circle 결과의 깊이는 같고 법선은 반대인지 확인
- Dynamic-Static, Dynamic-Dynamic 보정량 확인
- MTV 보정 후 벽까지의 정상 이동 거리와 접선 이동량이 유지되는지 확인
- 정적 Quadtree 결과가 브루트포스 AABB 결과와 같은지 무작위 입력으로 비교
- 노드 경계에 걸친 큰 충돌체가 누락되거나 중복되지 않는지 확인
- 동적 브루트포스가 자기 자신, 역순 쌍, 중복 쌍을 만들지 않는지 확인
- Layer/Mask와 비활성 바디 필터 확인

### 플레이 모드 테스트

- 벽을 정면 및 대각선으로 밀 때 침투 없이 멈춤/슬라이딩하는지 확인
- 안쪽 모서리와 바깥쪽 모서리에서 떨림이 없는지 확인
- 다수 Pawn과 벽이 있는 장면에서 후보 쌍 수와 프레임 시간을 측정
- 텔레포트 목적지가 벽 내부일 때 정해진 정책대로 밀려나는지 확인
- 낮은 프레임 또는 큰 시간 배율에서 이산 충돌 한계가 허용 범위인지 확인

## 17. 구현 순서

1. `ShapeType`, `CollisionShape2D`, `Contact`와 Circle-Circle/Circle-AABB 판정
2. 단위 테스트로 법선, 깊이, 내부 케이스 검증
3. Kinematic Body와 위치/속도 해소
4. 동적-동적 브루트포스로 작은 테스트 장면의 정답 동작 확보
5. 정적 Quadtree 구현 후 정적 브루트포스 결과와 비교 검증
6. Dynamic-Static Quadtree 질의와 Dynamic-Dynamic 브루트포스 결합
7. `KinematicBodyAuthoring`과 중앙 World 실행기 연결
8. `PawnMovement`와 `PawnCreator`에서 Rigidbody2D 의존 제거
9. 프로파일링 및 반복 횟수/노드 용량 튜닝
10. 필요 시 sub-step, 이후 sweep/CCD 확장

초기부터 Quadtree와 solver를 동시에 디버깅하지 않도록, 좁은 단계와 해소의 정답을 전수 비교 버전으로 먼저 확정한 뒤 정적 충돌체 조회만 Quadtree로 교체한다. 동적 충돌체는 1차 구현에서 브루트포스를 유지한다.

## 18. 초기 기본값

구현 시 별도 요구가 없다면 다음 값으로 시작한다.

- 시뮬레이션 주기: `Update`에서 이동 요청 생성, PostUpdate/PreLateUpdate에서 중앙 월드 시뮬레이션
- Solver 최대 반복: 4패스, 침투가 없으면 조기 종료
- 침투 `slop`: 사용하지 않음(0)
- Quadtree 노드 최대 아이템: 8
- Quadtree 최대 깊이: 6
- 정적 충돌체 광역 단계: Quadtree
- 동적 충돌체 광역 단계: 브루트포스
- Box 회전: 금지
- Circle 비균일 스케일: 금지
- 텔레포트: 목적지에서 겹침 해소
- 접촉 판정: 단순 접촉은 비침투, `depth > 0`인 겹침은 전체 깊이만큼 위치 보정

이 값들은 확정 사양이 아니라 측정과 플레이 감각을 위한 출발점이다.
