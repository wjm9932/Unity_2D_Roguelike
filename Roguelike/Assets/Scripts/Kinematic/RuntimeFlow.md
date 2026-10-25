# 실행 흐름과 위치 관리

<style>
table { border-collapse: collapse; }
table th + th, table td + td { border-left: 1px solid #808080; }
</style>

[개요](README.md) · [Sweep](Sweep.md) · [Discrete](Discrete.md)

## Kinematic 업데이트 시점 및 PlayerLoop 연결

`GameLoop`는 씬 로드 전에 Unity PlayerLoop의 최상위 `Update` 단계 바로 뒤에 메시지 처리, Pawn 이동 요청 소비, Kinematic Tick을 순서대로 삽입한다. 따라서 일반 Update 단계와 메시지 처리 단계에서 만든 이동 요청을 Pawn이 소비한 뒤, 같은 프레임의 Kinematic 단계에서 처리한다. `FixedUpdate` 주기로 실행하는 구조는 아니다. 전체 순서와 설치 방식은 [GameLoop README](../GameLoop/README.md)에 정리한다.

Kinematic 단계는 `KinematicWorld.Tick()`을 호출하고, 월드가 아직 초기화되지 않았다면 Tick은 Solve를 실행하지 않는다. Transform 반영은 이 단계에서 끝나므로 이후 `LateUpdate()`에서 최종 위치를 읽을 수 있다. Kinematic 단계 이후에 추가한 Move·Teleport 요청은 다음 Tick에서 처리한다.

## 요청과 계산 위치를 분리하는 방식

`KinematicBody`는 생성 시 Transform 위치를 계산용 기준 위치로 가져온다. 이후 충돌 계산에는 이 위치를 사용하고, Solve가 끝나면 Transform에 반영한다. Transform을 매번 읽어 충돌 계산의 기준으로 사용하는 방식은 아니다.

| 정보 | 역할 |
| --- | --- |
| 기준 위치 (`anchorPosition`) | 이동·충돌·Cast에서 사용하는 계산용 XY 위치 |
| 형상 중심 | 기준 위치에 `ColliderInfo.Offset`을 더한 충돌 중심 |
| 이동 요청 | 해당 처리 단계에서 사용할 누적 변위 |
| Teleport 요청 | 해당 처리 단계에서 적용할 목적지 |

위치가 `(2, 3)`인 Transform에 offset `(0.5, -0.2)`를 사용하면 충돌 중심은 `(2.5, 2.8)`이다. `Teleport()`의 목적지는 충돌 중심이 아니라 기준 위치다.

## 이동 요청

`Move(delta)`는 이번 Solve에 사용할 변위를 누적한다. 속도나 `deltaTime`을 내부에서 계산하지 않으므로, 이동을 요청하는 게임 로직이 변위를 계산해야 한다.

한 번의 Solve 전 `(1, 0)`과 `(0, 0.5)`를 요청하면 소비되는 이동량은 `(1, 0.5)`다. 이동 솔버가 요청을 가져갈 때 누적 값을 비워 다음 처리 단계에 다시 적용되지 않도록 한다.

`Teleport(position)`은 목적지를 저장한다. 여러 요청이 있으면 마지막 목적지를 사용한다. 바디 자체의 Teleport API는 이미 누적된 Move 요청을 지우지 않으므로, 둘 다 요청되었다면 Teleport 이후의 위치에서 Move를 처리한다.

현재 게임 로직인 `PawnMover`는 Teleport 요청을 처리하면 그 호출에서 Move 요청을 추가하지 않는다. 이것은 게임 로직의 요청 정책이며, 바디 API의 공통 규칙과는 구분한다.

## 바디의 접근 권한

`KinematicBody`는 인터페이스를 구현하지 않는 구체 클래스이며, 조회 프로퍼티·`SetTrigger()`·`Move()`·`Teleport()`를 public으로 제공한다. `OnTriggerEnter`, `OnTriggerStay`, `OnTriggerExit` 이벤트는 상대 바디의 `GameObject`를 전달한다. 사용하는 게임의 역할별 접근 방식을 바디의 상속 관계나 가시성에 넣지 않는다.

public 생성자로 실제 `KinematicBody`를 생성한다. 호출자는 바디를 직접 움직이거나 자신의 게임 로직에 맞는 별도 클래스로 감싸서 사용할 수 있다. 특정 프로젝트의 접근 제한을 위해 바디에 이동·상태 인터페이스를 추가하거나 생성자·이동 API를 감추지 않는다.

현재 게임의 접근 제한은 Pawn 쪽의 두 클래스가 담당한다. [PawnBody](../Pawn/Runtime/PawnBody.cs)는 조회 프로퍼티·`SetGhost()`·자기 형상의 `ShapeCast()`와 `ShapeCastAll()`만 제공하고, [PawnMotor](../Pawn/Runtime/PawnMotor.cs)는 `Move()`·`Teleport()`만 제공한다. `PawnBody.SetGhost()`는 대쉬 중 `IsTrigger`와 `TriggerLayer`를 함께 바꾸는 게임 측 편의 API다. 둘 다 실제 바디를 private으로 보관하며 서로 상속하지 않는다. `PawnCreator`는 `PawnMover`에 `PawnMotor`를, `PawnBehaviour`와 IdleSprint State에는 `IMoveRequestCollector`와 `PawnBody`를 전달한다. 전달받은 래퍼를 구체 바디나 다른 래퍼로 캐스팅할 수 없다. `Pawn`은 실제 바디를 보관해 월드 등록·해제를 담당한다.

`KinematicWorld.Register()`·`Unregister()`와 Cast API의 바디 인자는 구체 `KinematicBody`를 받고, `ShapeCastHit.Body`도 실제 대상 바디를 반환한다. 월드 경계에서 인터페이스를 구체 타입으로 캐스팅할 필요가 없다. `PawnBody`의 자기 형상 Cast는 내부의 실제 바디로 호출하므로, 자기 자신 제외도 기존 바디 참조 비교로 처리한다. Cast 결과는 게임 쪽 접근 제한을 적용한 객체로 변환하지 않는다.

## 한 번의 Solve

| 순서 | 처리 | 의도 |
| --- | --- | --- |
| 1 | 동적 바디의 Teleport 목적지 적용 | 이동 계산을 시작할 위치 결정 |
| 2 | 시작부터 겹친 바디의 접촉 해소 | 이동 경로 검사 전에 초기 침투 보정 |
| 3 | 선택한 이동 솔버 실행 | Sweep 또는 Discrete 방식으로 요청 변위 처리 |
| 4 | 동적 바디의 Transform 동기화 | 계산 위치와 Smooth 표시 오프셋을 게임 오브젝트에 반영 |
| 5 | 트리거 이벤트 발행 | Initial에서 기록한 현재 겹침 쌍을 이전 Tick과 비교해 Enter·Stay·Exit 통지 |

초기 겹침 해소와 이동 후 겹침 해소는 서로 다른 단계다. Sweep과 Discrete 모두 초기 겹침 해소를 거친다. 이동 후 겹침 해소는 Discrete 솔버에서 수행한다.

## 초기 겹침 해소

현재 위치의 접촉 판정으로 [MTV](CollisionAndMTV.md)를 구하고, 다음 정책으로 위치를 보정한다.

| 바디 쌍 | 보정 |
| --- | --- |
| 동적 A / 정적 B | A에 MTV 전체 적용 |
| 같은 우선순위의 동적 A / 동적 B | A에 MTV의 절반, B에 반대 방향 절반 적용 |
| 다른 우선순위의 동적 A / 동적 B | 낮은 바디에 MTV 전체 적용. 높은 바디의 보정은 0 |

동적 바디의 초기 보정은 이동 요청이나 질량의 비율을 사용하지 않는다. 같은 유효 우선순위의 두 바디가 정지해 있어도 시작부터 겹쳐 있다면 절반씩 분리한다. 같은 우선순위에서 한쪽만 트리거를 종료해 복구가 필요한 경우에는 종료한 바디만 보정한다. 우선순위가 다르면 복구 상태와 관계없이 낮은 바디에 MTV 전체를 적용한다.

Initial을 시작할 때 `overlapPushPriorities`에 각 바디의 원래 `Priority`를 채운다. 정적 접촉에서 실제 보정이 발생하면 `int.MaxValue`를, 동적 접촉에서는 실제로 보정된 바디에 더 높은 상대의 유효 우선순위를 전달한다. 접근 이동량은 요구하지 않는다. 따라서 같은 Initial 안에서는 벽의 권한과 연쇄 밀림이 후속 쌍의 우선순위에 영향을 준다.

Initial 종료 시 이 버퍼를 비운다. 이동 솔버는 자신의 `DiscreteBodyState` 또는 `SweepBodyState`를 원래 바디 우선순위로 생성한다. Initial과 이동이 공유하는 상태 객체나 중간 `Reset`은 없으며, Initial의 `int.MaxValue` 권한도 이동에 넘어가지 않는다.

정적 후보는 각 동적 바디의 현재 Bounds로 QuadTree를 조회한다. 동적 쌍은 목록에서 서로 다른 쌍을 한 번씩 검사한다. 한 보정이 다른 접촉을 만들 수 있으므로 정적 접촉과 동적 접촉 처리를 최대 2회 반복한다. 이는 `KinematicSolver`의 상수이며, Discrete 이동 단계의 설정값 16과 별개다. 한 회차에서 양쪽 모두 `TryCollide` 접촉을 찾지 못하면 종료한다. 별도 보정 경로 Cast, 막힘 기록, 실제 좌표 변경 여부를 확인하는 반복 종료 조건은 없다. 미세 침투는 `TryCollide`의 고정 허용 오차 `1e-5`로 제외하며, 그보다 커도 float 반올림으로 보정이 사라지면 반복 한도까지 같은 접촉이 남을 수 있다.

## Instant와 Smooth 보정

`ApplyInitialCorrection()`은 `OverlapResolutionMode`에 따라 보정 표시 방법을 선택한다. `Instant`는 `ApplyCorrection()`으로 계산 위치를 즉시 보정한다. `Smooth`는 `ApplySmoothCorrection()`으로 계산 위치를 즉시 보정하면서, 같은 보정의 반대 방향을 표시 오프셋에 누적한다.

```csharp
smoothCorrectionOffset -= correction;
smoothCorrectionStartOffset = smoothCorrectionOffset;
smoothCorrectionElapsed = 0f;
isSmoothingCorrection = true;
```

매 `SyncTransform()`에서 `Time.deltaTime`으로 경과 시간을 늘리고, 0.1초 동안 이차 ease-out을 적용한다. 현재 `InverseSmoothCorrectionDuration = 1f / 0.1f`는 `10f`와 같으며, 진행률은 경과 시간에 이 값을 곱해 구한다.

```csharp
var t = Mathf.Clamp01(smoothCorrectionElapsed * InverseSmoothCorrectionDuration);
var easeoutProgress = t * (2f - t);
smoothCorrectionOffset = smoothCorrectionStartOffset * (1f - easeoutProgress);
```

최종 Transform 위치는 `anchorPosition + smoothCorrectionOffset`이다. 시작할 때 빠르게 따라잡고 끝으로 갈수록 감속한다. 새 Initial 보정이 들어오면 남은 오프셋에 합산하고 진행 시간을 다시 시작한다. `t >= 1`이면 `isSmoothingCorrection`을 끄고 오프셋을 0으로 만든다. 충돌·Cast는 보정이 끝난 `anchorPosition`을 즉시 사용하므로, 스무딩 중 표시 위치와 계산 위치는 다를 수 있다. Discrete 이동 후 MTV는 `ApplyCorrection()`을 직접 호출하며 이 스무딩을 추가하지 않는다.

## 트리거와 복구

`SetTrigger(true)`는 바디를 통과 가능한 상태로 만든다. `TriggerLayer`는 이 트리거 바디가 어떤 상대 레이어를 통과할 수 있는지 정하며, 일반 `Physics2D` Layer Collision Matrix는 접촉 후보를 검사할지 결정한다. 트리거 쌍은 겹침 접촉으로 이벤트를 기록하지만 위치 보정에는 사용하지 않는다. 이동 경로의 Sweep Cast에서도 트리거 쌍을 충돌로 막지 않는다.

트리거 이벤트는 의도적으로 **Discrete 겹침 샘플링**으로 동작한다. 각 Kinematic Tick의 Solve 시작에서 Teleport 적용 후 Initial 겹침을 검사하고, 그때 실제로 겹친 쌍만 기록한다. 따라서 이동으로 새로 겹친 쌍의 Enter는 다음 Tick의 샘플에서 발생하고, 다음 샘플에도 겹쳐 있으면 Stay가 발생한다. 두 샘플 사이 이동 경로에만 잠시 존재하고 샘플 시점에는 겹치지 않는 트리거는 감지되지 않을 수 있다. 이는 현재 의도된 동작이며 경로 Sweep 기반의 연속 트리거 감지는 하지 않는다. 겹침이 사라진 쌍의 Exit도 다음 샘플 비교에서 발행된다.

쌍별 Enter·Stay·Exit은 두 바디 모두에 호출되며 인자는 상대 앵커의 `GameObject`다. `SetTrigger(false)`로 트리거 상태를 끝내면 `NeedTriggerRecovery`가 켜지고, 다음 Initial 겹침 해소에서 같은 우선순위 상대와 겹친 경우 종료 바디에 전체 MTV를 적용한다. 우선순위가 다른 쌍은 기존 우선순위 보정 규칙을 따른다. 복구 여부 판정은 레이어 마스크가 아니라 이 상태와 초기 접촉 보정 정책으로 처리한다.

## 결과 반영과 조회 시점

Solve가 진행되는 동안에는 계산 위치를 변경하며, 모든 동적 바디의 Transform은 마지막 동기화 단계에서 반영한다. Instant에서는 계산 위치를 그대로, Smooth 보정 중에는 표시 오프셋을 더해 반영한다.

`KinematicSolver`가 동적 바디 Transform의 Position을 전적으로 소유하며 점유한다. 매 프레임 바디의 계산 위치를 Transform에 동기화하므로, 외부 로직은 `Transform.position`을 통해 직접 변경하지 않고 `Move()` 또는 `Teleport()`로 위치 변경을 요청해야 한다. 따라서 외부에서 직접 변경한 Transform 위치는 바디의 계산 위치에 반영되지 않으며, 해당 프레임 Solve의 동기화 단계에서 덮어씌워진다.

Cast는 현재 계산 위치를 사용하고, 보관 중인 Move나 Teleport 요청을 미리 적용하지 않는다. 따라서 Solve 전 조회는 아직 요청이 반영되지 않은 위치를 기준으로 한다. Teleport 요청과 Cast 호출의 순서만으로 일부 바디의 조회 위치가 바뀌지 않도록 한 구조다.

## 등록과 해제

| 단계 | 책임 |
| --- | --- |
| 초기화 | `KinematicWorld.Init()`으로 맵 전체를 포함하는 Bounds와 이동 모드 지정 |
| 등록 | 정적 바디는 QuadTree, 동적 바디는 목록에 추가 |
| 등록 해제 | 해당 자료구조에서 바디 제거 |
| 월드 해제 | 트리와 목록 정리, 솔버 해제, 내부 참조와 초기화 상태 초기화 |

`KinematicBodyAdapter`는 씬에 배치되는 게임오브젝트에서 사용하는 컴포넌트로써 컴포넌트의 활성화·비활성화 시점에 등록·해제를 수행한다. 게임 로직에서 `new KinematicBody(...)`로 생성하는 경우에도 등록·해제는 그 바디를 사용하는 쪽에서 수행해야 한다. 생성자는 월드 등록을 수행하지 않는다.

월드는 등록과 Cast 전에 초기화되어 있어야 한다. 현재 재초기화는 기존 월드를 해제한 뒤 가능하지만, 초기화 전 해제나 중복 해제를 안전하게 무시하는 처리는 없다. 정적 바디는 등록 후 위치와 Bounds가 변하지 않는 전제로 사용한다.

## 관련 코드

- [KinematicBody](Runtime/KinematicBody.cs)
- [KinematicSolver](Runtime/KinematicSolver.cs)
- [KinematicSimulation](Runtime/KinematicSimulation.cs)
- [GameLoop](../GameLoop/GameLoop.cs)
- [KinematicBodyAdapter](Runtime/KinematicBodyAdapter.cs)
- [PawnMover](../Pawn/Runtime/PawnMover.cs)
