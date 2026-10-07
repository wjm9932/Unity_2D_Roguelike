# Kinematic 구현 방향과 구조

<style>
table { border-collapse: collapse; }
table th + th, table td + td { border-left: 1px solid #808080; }
</style>

## 문서 구성

이 문서는 모듈의 구현 방향과 책임을 설명한다. 이동과 충돌 알고리즘은 주제별 문서에서 계산 과정까지 다룬다. 설명은 현재 코드에 적용된 동작을 기준으로 한다.

| 문서 | 설명 |
| --- | --- |
| [실행 흐름](RuntimeFlow.md) | 이동 요청, Teleport, 초기 겹침 해소, PlayerLoop, Transform 반영 |
| [Shape Cast](ShapeCast.md) | 후보 조회, 형상 확장, 원 교차, Slab 검사, Rounded Box, 접촉 정보 |
| [접촉 판정과 MTV](CollisionAndMTV.md) | 현재 위치의 겹침 판정, 형상별 법선과 침투 깊이, 분리 벡터 |
| [Sweep 이동](Sweep.md) | 상대 이동량, 최초 충돌 시점, 동시 진행, 슬라이딩 |
| [Discrete 이동](Discrete.md) | 이동 후 겹침 해소, 동적 바디의 보정 분담 |
| [QuadTree](QuadTree.md) | 정적 바디 공간 분할, 삽입·조회·제거, 이동 경로 AABB |

## 구현 방향

게임 로직이 요청한 이동량을 받아 충돌을 처리하고, 결과 위치를 한 번에 Transform에 반영하는 2D 이동 모듈이다. 게임 로직은 원하는 이동을 결정하고, Kinematic은 그 이동을 충돌 형상에 맞게 제한하거나 겹침을 보정한다.

현재 지원 형상은 원과 축에 정렬된 박스다. 회전된 박스나 다각형은 지원하지 않으며, 충돌 형상에 Transform의 회전과 scale을 자동으로 반영하지 않는다. Unity의 `Rigidbody2D`가 이동을 계산하는 구조가 아니라, 직접 보관한 위치와 형상으로 이동 및 접촉을 계산한다.

## 구현 의도

### 게임 동작과 충돌 처리를 분리한다

이동 속도, 가속도, 입력 해석, 상태 전환은 게임 로직의 책임이다. Kinematic에는 한 번의 처리에 사용할 변위가 전달된다. 이를 통해 이동을 요청하는 방식과 충돌을 해결하는 방식을 따로 변경할 수 있도록 한다.

### 요청 시점과 적용 시점을 분리한다

`Move()`는 이동량을 누적하고 `Teleport()`는 목적지를 보관한다. 실제 위치 변경은 Solve 시점에 수행한다. 게임 로직이 요청을 만드는 도중에 일부 바디만 새 위치를 갖는 상황을 줄이고, 같은 처리 단계에서 위치를 갱신하려는 의도다.

### 외부 API와 내부 상태의 소유권을 구분한다

외부에는 월드 초기화·등록·해제용 `KinematicWorld`, 조회용 `Kinematics`, 개별 바디의 이동 요청 API를 제공한다. 바디의 위치 보정, 요청 소비, 월드 Tick과 솔버 구현은 모듈 내부에서 관리한다.

`KinematicSimulation`이 정적 바디 트리와 동적 바디 목록을 소유한다. 솔버와 Cast에는 조회 인터페이스 및 읽기 전용 목록을 전달하여 등록·제거 책임까지 넘기지 않는다. 바디 자체의 위치는 솔버가 변경할 수 있지만, 읽기 전용 목록을 통해 목록의 구성은 변경하지 못한다.

### 공통 처리와 이동 알고리즘을 분리한다

Teleport 적용, 초기 겹침 해소, Transform 동기화는 `KinematicSolver`에서 공통으로 수행한다. 이동 방식은 `IKinematicMovementSolver`를 구현하는 Sweep 또는 Discrete 솔버가 담당한다. 이동 알고리즘을 바꾸더라도 공통 실행 흐름은 유지하려는 구조다.

### 필요한 부분만 Unity 컴포넌트와 연결한다

바디와 솔버는 일반 C# 클래스로 구현한다. `KinematicBodyAdapter`는 Inspector의 형상 설정과 게임 오브젝트의 활성화·비활성화를 등록·해제로 연결하는 역할을 맡는다. 이동 처리는 바디별 `MonoBehaviour.Update()` 대신 하나의 PlayerLoop 단계에서 실행한다.

## 구조와 책임

| 구성 요소 | 책임 |
| --- | --- |
| `KinematicWorld` | 외부에서 사용할 월드 관리 API 제공 |
| `Kinematics` | 외부에서 사용할 Cast 조회 API 제공 |
| `KinematicSimulation` | 월드 상태와 바디 목록 소유, 초기화·등록·해제, 솔버 및 Cast 구성 |
| `KinematicPlayerLoop` | Unity PlayerLoop에 공통 이동 처리 단계 연결 |
| `KinematicBody` | 계산용 위치와 형상 보관, 이동·Teleport 요청 보관, 결과 위치 반영 |
| `KinematicBodyAdapter` | Unity 컴포넌트 생명주기와 바디 등록 연결 |
| `KinematicSolver` | Teleport, 초기 겹침 해소, 이동 솔버 실행, Transform 동기화 |
| `KinematicSweepSolver` | 이동 경로의 충돌 시점 계산과 잔여 이동 조정.|
| `KinematicDiscreteSolver` | 이동 후 겹침을 검사하고 MTV 보정 분담.|
| `KinematicCast` | 정적·동적 후보를 조회하고 가장 가까운 Cast 결과 선택 |
| `KinematicShapeCast` | 한 형상 쌍의 이동 경로와 최초 접촉 계산 |
| `KinematicBodyExtension` | 한 형상 쌍의 현재 겹침과 분리 정보 계산 |
| `QuadTree` | 정적 바디의 AABB 후보 조회 |

## Sweep과 Discrete의 동작 차이

속도에 `dt`를 곱해 이동을 요청하면, `dt`나 이동 속도가 커질수록 한 번에 처리하는 변위도 커진다. 솔버는 `dt` 자체를 받는 대신 이렇게 계산된 변위를 처리한다.

| 구분 | Sweep | Discrete |
| --- | --- | --- |
| 충돌 검사 범위 | 시작 위치에서 도착 위치까지의 이동 경로 | 이동을 적용한 뒤의 위치 |
| 장애물을 지나치는 큰 변위 | 경로 중간의 접촉을 검출하고 최초 접촉 위치까지 이동 | 도착 위치가 장애물과 겹치지 않으면 중간 충돌을 감지하지 못함 |
| `dt`·속도 증가의 영향 | 이동량이 커져도 이동 경로를 검사하므로 도착 위치만 검사해서 생기는 누락을 방지 | 큰 이동량으로 얇은 장애물을 통과하는 tunneling이 발생할 수 있음 |
| 접촉 후 처리 | 남은 변위에서 표면 안쪽 성분을 제거해 슬라이딩 | 현재 겹침을 MTV로 분리 |

Sweep의 차이는 이동 경로를 검사한다는 데 있다. 현재 지원 형상과 직선 이동을 기준으로 하며, 모든 조건에서 충돌 감지를 보장한다는 의미는 아니다. 수치 오차, 후보 등록과 반복 제한도 결과에 영향을 준다. 자세한 계산과 예시는 [Sweep](Sweep.md)과 [Discrete](Discrete.md)에 정리한다.

## 폴더와 의존성

| 폴더 | 내용과 의존 관계 |
| --- | --- |
| `Data` | `AABB`, `ColliderInfo`, `Contact` 등 공통 데이터 |
| `Interface` | 조회·이동 솔버 계약. `Data` 참조 |
| `Runtime` | 바디, 월드, 솔버, Cast, 공간 분할 구현. `Data`와 `Interface` 참조 |
| `Test` | 테스트 입력으로 바디를 움직이는 컴포넌트 |

## 현재 적용 범위

이 모듈은 요청된 변위와 겹침을 기하학적으로 처리한다. 질량, 마찰, 반발력, 힘과 관성을 계산하는 물리 솔버는 아니다. 동적 바디의 처리 순서와 반복 횟수 제한에 따라 결과가 달라질 수 있으며, 모든 겹침이 반드시 해소된다고 보장하지 않는다.

현재 `Bootstrap`은 `useSweep: false`로 초기화하므로 기본 실행은 Discrete다. 모드 선택은 월드 초기화 시 이루어지며, 초기화된 월드의 솔버를 런타임에 바꾸는 API는 없다.

등록과 Cast 사용 전에 월드 초기화가 필요하다. 바디 등록의 중복 방지, 정적 바디 이동 시 트리 갱신, 초기화 전 또는 반복 `Dispose()` 호출에 대한 보호는 현재 구현에 포함되어 있지 않다. 자세한 사용 조건은 [실행 흐름](RuntimeFlow.md)과 [QuadTree](QuadTree.md)에 정리한다.
