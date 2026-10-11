# GameLoop 실행 순서

`GameLoop`는 Unity PlayerLoop에 게임의 공통 처리 단계를 등록한다. 일반 `Update`에서 요청을 모은 뒤 메시지 전달, Pawn 이동 요청 소비, Kinematic 이동 계산을 순서대로 실행한다.

## 프레임 내 업데이트 타이밍

`GameLoop.Install()`은 씬 로드 전(`BeforeSceneLoad`)에 실행된다. `DefineUpdateOrder()`에 정의한 단계를 Unity PlayerLoop의 최상위 `Update` 바로 뒤에 삽입한다. `MonoBehaviour.Update()`를 포함한 Update 단계가 모두 끝난 뒤, `PreLateUpdate`와 그 안의 `MonoBehaviour.LateUpdate()`보다 먼저 실행한다. `FixedUpdate` 간격과 별개로 프레임마다 실행한다.

| 순서 | 단계 | 호출과 역할 |
| --- | --- | --- |
| 1 | Unity `Update` | Pawn 상태·행동 등 게임 로직에서 이벤트와 이동 요청 생성 |
| 2 | `MessageSystemUpdate` | `MessageSystem.Instance.Update()`로 구독·해제 요청 반영 및 대기 이벤트 전달 |
| 3 | `PawnMovementResolveUpdate` | `PawnExtensions.ConsumeMoveRequests()`로 등록된 Pawn의 이동 요청 소비 |
| 4 | `KinematicWorldUpdate` | `KinematicWorld.Tick()`으로 충돌 처리 및 동적 바디의 최종 Transform 반영 |
| 5 | Unity `PreLateUpdate` | `MonoBehaviour.LateUpdate()` 등에서 이동 결과 사용 |

메시지 처리 단계에서 생성한 Pawn 이동 요청도 바로 다음 이동 요청 소비 단계에 전달된다. PawnMover는 요청을 처리해 변위를 계산하고 `KinematicBody.Move()` 또는 `Teleport()`에 전달한다. 이어지는 Kinematic 단계에서 해당 요청을 적용하므로, 이 흐름으로 생성한 이동 요청은 같은 프레임에 위치까지 반영된다.

Kinematic Solve 전에 Transform을 읽거나 Cast를 호출하면 아직 그 요청이 반영되지 않은 위치를 사용한다. `LateUpdate()`에서 위치를 읽으면 Kinematic이 반영한 결과를 사용할 수 있다. 이미 해당 소비·처리 단계를 지난 뒤 생성한 요청은 다음 프레임의 해당 단계에서 처리한다.

## PlayerLoop 등록과 정리

설치 시 기존 `MessageSystemUpdate`, `PawnMovementResolveUpdate`, `KinematicWorldUpdate` 항목을 제거한 뒤 다시 삽입하여 중복 등록을 방지한다. 삽입 대상 `Update`를 찾지 못하면 오류를 기록하고 PlayerLoop를 변경하지 않는다.

Editor에서는 종료 시 등록한 단계를 제거한다. Edit 모드에서 스크립트가 다시 로드되는 경우에도 남아 있는 단계를 정리한다. Kinematic 월드 초기화는 별도로 필요하며, 초기화되지 않은 월드의 Tick은 Solve를 실행하지 않는다.

## 관련 문서와 코드

- [Kinematic 개요 및 업데이트 타이밍](../Kinematic/README.md)
- [Kinematic 요청 처리와 Solve 흐름](../Kinematic/RuntimeFlow.md)
- [GameLoop](GameLoop.cs)
- [MessageSystem](../EventSystem/MessageSystem.cs)
- [PawnExtensions](../Pawn/Runtime/PawnExtensions.cs)
- [PawnMover](../Pawn/Runtime/PawnMover.cs)
- [KinematicWorld](../Kinematic/Runtime/KinematicWorld.cs)
