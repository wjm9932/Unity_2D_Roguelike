# 실행 흐름과 위치 관리

<style>
table { border-collapse: collapse; }
table th + th, table td + td { border-left: 1px solid #808080; }
</style>

[개요](README.md) · [Sweep](Sweep.md) · [Discrete](Discrete.md)

## Kinematic 업데이트 시점 및 PlayerLoop 연결

`KinematicPlayerLoop`는 씬 로드 전에 Unity PlayerLoop의 최상위 `Update` 단계 바로 뒤에 Kinematic 처리 단계를 삽입한다. 따라서 일반 Update 단계에서 누적한 요청을 그 뒤의 Kinematic 단계에서 처리한다. `FixedUpdate` 주기로 실행하는 구조는 아니다.

설치 시 기존 Kinematic 단계를 제거한 뒤 다시 넣어 중복 실행을 방지한다. PlayerLoop는 월드의 Tick을 호출하고, 월드가 아직 초기화되지 않았다면 Tick은 Solve를 실행하지 않는다.

## 요청과 계산 위치를 분리하는 방식

`KinematicBody`는 생성 시 Transform 위치를 계산용 기준 위치로 가져온다. 이후 충돌 계산에는 이 위치를 사용하고, Solve가 끝나면 Transform에 반영한다. Transform을 매번 읽어 충돌 계산의 기준으로 사용하는 방식은 아니다.

| 정보 | 역할 |
| --- | --- |
| 기준 위치 | 게임 오브젝트를 배치할 계산용 XY 위치 |
| 형상 중심 | 기준 위치에 `ColliderInfo.Offset`을 더한 충돌 중심 |
| 이동 요청 | 해당 처리 단계에서 사용할 누적 변위 |
| Teleport 요청 | 해당 처리 단계에서 적용할 목적지 |

위치가 `(2, 3)`인 Transform에 offset `(0.5, -0.2)`를 사용하면 충돌 중심은 `(2.5, 2.8)`이다. `Teleport()`의 목적지는 충돌 중심이 아니라 기준 위치다.

## 이동 요청

`Move(delta)`는 이번 Solve에 사용할 변위를 누적한다. 속도나 `deltaTime`을 내부에서 계산하지 않으므로, 이동을 요청하는 게임 로직이 변위를 계산해야 한다.

한 번의 Solve 전 `(1, 0)`과 `(0, 0.5)`를 요청하면 소비되는 이동량은 `(1, 0.5)`다. 이동 솔버가 요청을 가져갈 때 누적 값을 비워 다음 처리 단계에 다시 적용되지 않도록 한다.

`Teleport(position)`은 목적지를 저장한다. 여러 요청이 있으면 마지막 목적지를 사용한다. 바디 자체의 Teleport API는 이미 누적된 Move 요청을 지우지 않으므로, 둘 다 요청되었다면 Teleport 이후의 위치에서 Move를 처리한다.

현재 게임 로직인 `PawnMover`는 Teleport 요청을 처리하면 그 호출에서 Move 요청을 추가하지 않는다. 이것은 게임 로직의 요청 정책이며, 바디 API의 공통 규칙과는 구분한다.

## 한 번의 Solve

| 순서 | 처리 | 의도 |
| --- | --- | --- |
| 1 | 동적 바디의 Teleport 목적지 적용 | 이동 계산을 시작할 위치 결정 |
| 2 | 시작부터 겹친 바디의 접촉 해소 | 이동 경로 검사 전에 초기 침투 보정 |
| 3 | 선택한 이동 솔버 실행 | Sweep 또는 Discrete 방식으로 요청 변위 처리 |
| 4 | 동적 바디의 Transform 동기화 | 최종 계산 위치를 게임 오브젝트에 반영 |

초기 겹침 해소와 이동 후 겹침 해소는 서로 다른 단계다. Sweep과 Discrete 모두 초기 겹침 해소를 거친다. 이동 후 겹침 해소는 Discrete 솔버에서 수행한다.

## 초기 겹침 해소

현재 위치의 접촉 판정으로 [MTV](CollisionAndMTV.md)를 구하고, 다음 정책으로 위치를 보정한다.

| 바디 쌍 | 보정 |
| --- | --- |
| 동적 A / 정적 B | A에 MTV 전체 적용 |
| 동적 A / 동적 B | A에 MTV의 절반, B에 반대 방향 절반 적용 |

동적 바디의 초기 보정은 이동 요청이나 질량의 비율을 사용하지 않는다. 두 바디가 정지해 있어도 시작부터 겹쳐 있다면 절반씩 분리한다.

정적 후보는 각 동적 바디의 현재 Bounds로 QuadTree를 조회한다. 동적 쌍은 목록에서 서로 다른 쌍을 한 번씩 검사한다. 한 보정이 다른 접촉을 만들 수 있으므로 정적 접촉과 동적 접촉 처리를 반복한다. 최대 16회이며, 한 회차에서 둘 다 접촉을 찾지 못하면 종료한다.

## 결과 반영과 조회 시점

Solve가 진행되는 동안에는 계산 위치를 변경하며, 모든 동적 바디의 Transform은 마지막 동기화 단계에서 반영한다. 보정도 최종 위치에 포함된다.

`KinematicSolver`는 동적 바디의 Transform 위치 제어를 점유한다. 매 프레임 바디의 계산 위치를 Transform에 동기화하므로, 외부 로직은 `Transform.position`을 통해 직접 변경하지 않고 `Move()` 또는 `Teleport()`로 위치 변경을 요청해야 한다. 따라서 외부에서 직접 변경한 Transform 위치는 바디의 계산 위치에 반영되지 않으며, 해당 프레임 Solve의 동기화 단계에서 덮어씌워진다.

Cast는 현재 계산 위치를 사용하고, 보관 중인 Move나 Teleport 요청을 미리 적용하지 않는다. 따라서 Solve 전 조회는 아직 요청이 반영되지 않은 위치를 기준으로 한다. Teleport 요청과 Cast 호출의 순서만으로 일부 바디의 조회 위치가 바뀌지 않도록 한 구조다.

## 등록과 해제

| 단계 | 책임 |
| --- | --- |
| 초기화 | `KinematicWorld.Init()`으로 맵 전체를 포함하는 Bounds와 이동 모드 지정 |
| 등록 | 정적 바디는 QuadTree, 동적 바디는 목록에 추가 |
| 등록 해제 | 해당 자료구조에서 바디 제거 |
| 월드 해제 | 트리와 목록 정리, 솔버 해제, 내부 참조와 초기화 상태 초기화 |

`KinematicBodyAdapter`는 씬에 배치되는 게임오브젝트에서 사용하는 컴포넌트로써 컴포넌트의 활성화·비활성화 시점에 등록·해제를 수행한다. 게임 로직에서 바디를 직접 생성하는 경우에도 등록·해제는 그 바디를 사용하는 쪽에서 수행해야 한다.

월드는 등록과 Cast 전에 초기화되어 있어야 한다. 현재 재초기화는 기존 월드를 해제한 뒤 가능하지만, 초기화 전 해제나 중복 해제를 안전하게 무시하는 처리는 없다. 정적 바디는 등록 후 위치와 Bounds가 변하지 않는 전제로 사용한다.

## 관련 코드

- [KinematicBody](Runtime/KinematicBody.cs)
- [KinematicSolver](Runtime/KinematicSolver.cs)
- [KinematicSimulation](Runtime/KinematicSimulation.cs)
- [KinematicPlayerLoop](Runtime/KinematicPlayerLoop.cs)
- [KinematicBodyAdapter](Runtime/KinematicBodyAdapter.cs)
- [PawnMover](../Pawn/Runtime/PawnMover.cs)
