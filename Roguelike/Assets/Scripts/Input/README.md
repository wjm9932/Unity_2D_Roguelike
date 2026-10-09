# InputManager 구현 방향과 구조

<style>
table {
    border-collapse: collapse;
}
table th + th,
table td + td {
    border-left: 1px solid #808080;
}
</style>

## 구현 방향

입력 모듈은 키 입력을 받아 게임 로직이 사용할 입력 상태로 전달한다. 이동 속도 계산, 상태 전환, 이동 요청 생성과 같은 게임 동작은 입력을 사용하는 쪽에서 처리한다.

`InputManager`는 입력 객체의 생성과 해제를 담당하고, `PawnControls`는 Input System 콜백을 폰 입력 상태로 변환한다. 외부에는 `IPawnControls`를 제공하여 입력 상태 조회와 입력 수신 제어만 허용한다.

이 문서는 현재 코드의 책임 분리와 동작을 기준으로 구현 의도를 설명한다. 앞으로 확장할 수 있는 방향과 이미 구현된 기능은 구분해서 적는다.

## 구현 의도

### MonoBehaviour 대신 일반 C# 클래스를 사용하는 이유

이 입력 처리에는 `Transform`, Inspector 설정, 게임 오브젝트에 부착되는 컴포넌트 기능이 필요하지 않다. 생성된 `RogueLikeInput` 객체로 액션 맵을 활성화하고 콜백을 등록하면 입력을 받을 수 있으므로, 입력 처리를 위한 별도의 `MonoBehaviour.Update()`도 필요하지 않다.

따라서 입력 객체를 씬에 배치하거나 `Awake`, `OnEnable`, `OnDestroy`에 초기화와 해제를 연결하는 대신, 일반 클래스에서 생성·등록·해제를 관리한다. 이는 컴포넌트로 동작해야 하는 경우가 아니라면 일반 C# 클래스를 사용하는 프로젝트 규칙과도 일치한다.

이 구조에서 얻는 이점은 다음과 같다.

- 씬에 입력 매니저 오브젝트를 배치해야 한다는 의존성이 없다.
- 폰이나 씬마다 입력 액션을 생성하고 콜백을 등록하는 코드를 반복하지 않는다.
- 입력 콜백 등록·해제와 관련 호출을 한곳에서 관리해, 입력 처리 코드가 여러 클래스에 흩어지는 것을 막는다.
- 입력 객체의 소유권은 `InputManager`, 입력 상태 변환은 `PawnControls`에 두고, 실제 게임 동작은 입력을 사용하는 게임 로직에서 처리한다.

여기서 일반 클래스라는 말은 Unity 컴포넌트 생명주기를 상속하지 않는다는 의미다. `Vector2`, Input System, R3를 사용하므로 Unity와 무관하게 실행되는 독립 모듈은 아니다. 또한 `MonoBehaviour`의 자동 생명주기를 사용하지 않는 만큼, 최종 해제 시점은 별도로 연결해야 한다.

### 입력 상태를 게임 동작과 분리하는 이유

키 바인딩과 `InputAction.CallbackContext`를 게임 로직까지 전달하면 각 컨트롤러와 상태가 Input System의 액션 이름, 이벤트 단계, 값 읽기 방식을 알아야 한다.

`PawnControls`는 이를 `MoveInfo`로 변환한다. 게임 로직은 `Move.CurrentValue`를 읽어 방향과 이동 여부를 판단하고, 각 클래스의 책임에 맞게 사용한다. 예를 들어 `PawnIdleSprintState`가 이동 속도를 곱해 `MoveRequest`를 생성하며, 입력 모듈 자체는 이동 요청을 만들지 않는다.

### 외부에 IPawnControls를 제공하는 이유

게임 로직에 필요한 기능만 제공하고, 외부에서 접근할 수 있는 API를 최소화하려는 의도다. 입력 상태 조회와 `Enable()` / `Disable()`을 통한 입력 수신 제어만 `IPawnControls`에 포함한다.

입력 모듈 내부에서만 사용하는 메서드는 이 인터페이스에 공개하지 않는다. `Dispose()`도 `IPawnControls`에 포함하지 않아, 게임 로직이 공유 입력 자원을 직접 해제하지 않도록 한다. 입력 자원의 최종 해제는 소유자인 `InputManager`가 담당한다.

이를 위해 `InputManager.PawnControls`의 반환 타입은 `IPawnControls`로 두고, 실제 구현인 `PawnControls`는 `internal`로 제한한다. 게임 로직은 구체적인 구현 대신 필요한 기능을 정의한 인터페이스를 사용한다.

### 입력 콜백을 Adapter로 분리하는 이유

`IPawnControls`에는 콜백 메서드가 없으므로 이 인터페이스를 통한 일반적인 접근에서는 `OnMove()`를 호출할 수 없다. 다만 `PawnControls` 자체가 생성 래퍼의 공개 인터페이스인 `IPawnActions`를 구현하면, 외부에 제공된 객체를 `IPawnActions`로 캐스팅하여 콜백을 직접 호출할 수 있다.

이러한 접근 가능성까지 없애기 위해, `IPawnActions`는 중첩된 private 클래스 `PawnInputAdapter`가 구현하도록 의도적으로 분리한다. `PawnControls` 자체는 콜백 인터페이스를 구현하지 않고 Adapter도 외부에 제공하지 않으므로, 공개된 Controls 객체를 콜백 인터페이스로 캐스팅하는 접근 경로를 차단한다.

또한 클래스를 의도적으로 분리해 책임을 나누려는 의도도 포함되어 있다. `PawnControls`는 입력 상태를 보관하고 외부에 제공하는 역할을 맡고, Adapter는 Input System의 콜백을 등록·해제하고 입력 상태 갱신으로 연결하는 역할을 맡는다.

## 구조와 책임

| 구성 요소 | 책임 |
| --- | --- |
| `InputManager` | 공유 인스턴스 제공, `RogueLikeInput`과 `PawnControls` 생성, 액션 맵 활성화, 최종 해제 |
| `RogueLikeInput` | `.inputactions`에서 생성된 래퍼. 액션·바인딩 정의와 `Pawn` 맵, 콜백 등록 API 제공 |
| `IPawnControls` | 외부에서 사용할 입력 상태 조회 및 입력 수신 제어 계약 |
| `PawnControls` | R3 입력 상태 보관, 입력 수신 제어, 상태 해제 |
| `PawnInputAdapter` | `IPawnActions` 구현, 콜백 등록·해제, 콜백 값을 `MoveInfo`로 변환 |
| `MoveInfo` | 입력 벡터 `RawValue`와 이동 여부 `IsMoving`을 보관하는 값 타입 |
| 게임 로직 | 입력 상태를 읽어 컨트롤러 상태 갱신, 이동 요청 생성 등 게임 동작 처리 |

객체 소유 관계는 다음과 같다.

| 소유자 | 소유하거나 관리하는 대상 | 관계 |
| --- | --- | --- |
| `InputManager` | `RogueLikeInput` | 입력 래퍼를 생성하고 최종 해제한다. |
| `RogueLikeInput` | 내부 입력 에셋과 `Pawn` 액션 맵 | `Move`, `TestMove` 액션을 제공한다. |
| `InputManager` | `PawnControls` | 입력 상태 객체를 생성하고 외부에는 `IPawnControls`로 제공한다. |
| `PawnControls` | `ReactiveProperty<MoveInfo>` | 입력 상태를 보관하고 외부에는 읽기 전용으로 제공한다. |
| `PawnControls` | `PawnInputAdapter` | Adapter를 생성하여 입력 콜백의 등록과 해제를 맡긴다. |
| `PawnInputAdapter` | `PawnControls`와 `PawnActions`의 참조 | `IPawnActions`를 구현하고 액션 콜백을 입력 상태 갱신으로 연결한다. |

입력 상태가 전달되는 흐름은 다음과 같다.

| 순서 | 처리 주체 | 처리 내용 | 전달되는 결과 |
| --- | --- | --- | --- |
| 1 | Input System | 키 입력을 바인딩에 따라 `Pawn/Move` 또는 `Pawn/TestMove` 액션으로 처리한다. | 액션 이벤트 |
| 2 | `PawnInputAdapter` | 등록된 `OnMove()` 또는 `OnTestMove()` 콜백을 받는다. | `InputAction.CallbackContext` |
| 3 | `PawnInputAdapter` | `Vector2` 값을 읽어 `MoveInfo`를 생성한다. | `RawValue`, `IsMoving` |
| 4 | `PawnControls` | 해당 `ReactiveProperty`에 새로운 입력 상태를 저장한다. | 외부에서 읽을 수 있는 현재 입력 상태 |
| 5 | 게임 로직 | `IPawnControls`를 통해 입력 상태를 읽고 게임 동작에 사용한다. | 컨트롤러 상태 갱신 또는 이동 요청 등 |

`ReadOnlyReactiveProperty<MoveInfo>`는 현재 상태 조회와 변경 구독을 제공한다. 현재 `ManualPawnController`와 `PawnIdleSprintState`는 구독 대신 자신의 업데이트 시점에 `Move.CurrentValue`를 읽는다.

## 구현 방식

### 생성과 활성화

`InputManager`는 처음 사용할 때 생성되며, 입력 자원과 Controls를 준비한다. 매니저가 입력 액션 맵의 활성화를 관리하고, 각 Controls의 Adapter가 담당 액션에 콜백을 연결하여 입력을 받을 수 있도록 한다.

초기화는 `InputManager`에서 관리하며, 게임 로직은 준비된 Controls의 공개 API를 사용한다. 씬이나 입력을 사용하는 클래스마다 입력 자원 생성과 콜백 등록을 반복하지 않도록 한다.

### 입력 정의와 생성 코드

입력 정의의 원본은 [`../Etc/RogueLikeInput.inputactions`](../Etc/RogueLikeInput.inputactions)이고, 생성 결과는 [`RogueLikeInput.cs`](RogueLikeInput.cs)다. 원본 에셋의 importer 설정이 래퍼 생성 위치를 이 Input 폴더로 지정한다.

현재 바인딩은 다음과 같다.

| 액션 | 바인딩 | 사용처 |
| --- | --- | --- |
| `Pawn/Move` | 방향키의 `2DVector` composite | `ManualPawnController`, `PawnIdleSprintState` |
| `Pawn/TestMove` | WASD의 `2DVector` composite | `KinematicDynamicMoveTest` |

이 경로는 `new RogueLikeInput()`으로 만든 액션 인스턴스를 사용한다. 래퍼 생성자는 내부 JSON 정의를 `InputActionAsset.FromJson()`으로 읽어 새 에셋을 만든다. Project-wide Actions로 설정된 에셋과 원본 정의가 같더라도, `InputSystem.actions`에서 얻는 객체를 직접 사용하는 구조는 아니다.

액션과 바인딩을 바꿀 때는 원본 `.inputactions`를 수정하고 래퍼를 재생성한다. `RogueLikeInput.cs`를 직접 수정하면 재생성 시 변경 내용이 사라질 수 있다.

### 콜백 연결

Adapter는 생성 래퍼의 콜백 등록 API를 통해 입력 액션에 연결된다. 입력 콜백을 받으면 담당 Controls가 사용할 입력 상태로 변환하여 갱신한다. 입력 값의 타입과 처리 방식은 각 Adapter가 담당하는 액션에 따라 결정한다.

### Enable과 Disable의 의미

액션 맵 활성화와 `IPawnControls`의 입력 수신 제어는 역할이 다르다.

액션 맵을 비활성화하는 대신 콜백을 등록·해제하는 이유는 각 Controls에 해당하는 입력 수신만 개별적으로 제어하기 위해서다. 각 Controls의 Adapter가 `Register()`로 자신의 콜백을 등록하고, 비활성화할 때는 그 Adapter의 콜백만 해제하는 방식이다.

`Pawn` 액션 맵을 비활성화하면 같은 `rogueLikeInput` 인스턴스의 `Pawn` 맵을 구독하는 다른 Adapter의 입력 수신도 중단된다. 따라서 해당 Adapter의 콜백만 해제하여, 같은 `Pawn` 액션 맵을 사용하는 다른 Controls의 입력 수신에 영향을 주지 않도록 한다.

따라서 액션 맵의 활성화와 최종 비활성화는 입력 자원을 소유한 `InputManager`가 담당하고, 개별 Controls의 `Enable()` / `Disable()`은 자신의 Adapter를 통한 입력 수신만 제어하도록 책임을 나눈다.

- `rogueLikeInput.Pawn.Enable()` / `Disable()`은 해당 `rogueLikeInput` 인스턴스의 `Pawn` 액션 맵에 포함된 모든 액션을 활성화하거나 비활성화한다.
- `PawnControls.Enable()`은 먼저 `Disable()`을 호출해 혹시 모를 중복 등록을 방지하기 위해 해당 `PawnControls` 객체가 자신의 Adapter를 통해 등록한 콜백을 먼저 해제한 뒤, 그 콜백을 다시 등록한다.
- `PawnControls.Disable()`은 해당 `PawnControls` 객체가 자신의 Adapter를 통해 등록한 콜백만 해제하고, `Move`를 기본값으로 되돌린다. `Pawn` 액션 맵의 활성화 상태는 변경하지 않는다.

따라서 `IPawnControls.Disable()`은 해당 `PawnControls` 객체가 자신의 Adapter 콜백을 통해 입력 상태를 갱신하는 것을 중단하는 동작이다.

두 방식의 차이는 다음과 같다.

| 구분 | 액션 맵 비활성화 | Adapter 콜백 해제 |
| --- | --- | --- |
| 액션의 입력 처리 | 해당 맵의 모든 액션이 중단 | 맵이 활성화되어 있으면 계속 처리 |
| 다른 수신자 | 같은 액션 인스턴스의 다른 수신자도 이후 입력 이벤트를 받지 못함 | 해제한 Adapter 외의 수신자는 계속 이벤트를 받음 |
| 콜백 등록 상태 | 기존 등록 유지 | 해당 Adapter의 등록 제거 |
| 진행 중인 입력 | 비활성화 과정에서 `canceled`가 발생할 수 있음 | 등록 해제 자체는 액션을 취소하지 않음 |
| 다시 수신 | 액션 맵 활성화 | Adapter 콜백 재등록 |

이 차이는 같은 액션 인스턴스를 기준으로 한다. 다른 에셋 인스턴스나 다른 맵의 입력까지 중단하는 것은 아니다. 또한 콜백 해제 자체가 R3 상태를 초기화하는 것은 아니며, 현재 `Move` 초기화는 `move.Value = default`로 직접 수행한다.

콜백을 재등록하는 것만으로 현재 액션 값이 즉시 전달되지는 않는다. 예를 들어 방향키를 계속 누른 채 `Disable()`과 `Enable()`을 호출하면, 초기화된 `Move`가 다음 액션 이벤트까지 기본값으로 남을 수 있다. 반면 현재 `Move`처럼 초기 상태 검사를 사용하는 `Value` 액션은 액션 맵을 다시 활성화한 뒤 다음 Input System 업데이트에서 이미 눌려 있는 키를 검사한다.

### 해제와 생명주기

`InputManager.Dispose()`는 다음 순서로 해제한다.

1. `PawnControls.Dispose()`에서 콜백을 해제하고 `Move` 상태를 초기화한 뒤 R3 프로퍼티를 해제한다.
2. `Pawn` 액션 맵을 비활성화한다.
3. 생성 래퍼를 해제하여 내부 입력 에셋을 제거한다.

`Dispose()`는 `IPawnControls`에 포함하지 않는다. 공유 입력 자원의 소유자인 매니저가 해제를 담당하도록 구분한 것이다.

현재 프로젝트 코드에는 `InputManager.Dispose()`를 호출하는 종료 경로가 연결되어 있지 않다. 또한 해제 후 static `instance`를 초기화하거나 입력 객체를 다시 생성하는 처리도 없다. 따라서 자동 종료 해제나 해제 후 재사용이 지원된다고 가정하면 안 된다.

## 현재 구현의 범위와 확인할 부분

일반 클래스와 인터페이스를 사용한 책임 분리는 적용되어 있지만, 입력 구현 교체, 사용자별 입력 인스턴스, 자동 종료 해제까지 포함하는 구조는 아니다. 확장 시에는 이 기능들을 별도로 설계해야 한다.

`TestMove`는 테스트용 입력이며 `IPawnControls`의 프로퍼티와 `PawnControls`의 상태 필드는 `UNITY_EDITOR` 조건 안에 있다. 다만 `OnTestMove()`와 테스트 코드의 입력 접근은 같은 조건으로 감싸져 있지 않다. Editor에서의 컴파일 확인과 별개로 Player 빌드에서는 이 참조 범위를 확인해야 한다.

또한 현재 `Disable()`과 `Dispose()`는 `Move`만 초기화·해제한다. Editor의 `TestMove` 상태까지 같은 방식으로 처리하지 않으므로, 문서의 일반 입력 생명주기 설명을 테스트 입력에도 동일하게 적용해서는 안 된다.

## 관련 코드

- [`InputManager.cs`](InputManager.cs): 생성, 공유 접근, 최종 해제
- [`Controls/PawnControls.cs`](Controls/PawnControls.cs): 인터페이스, 입력 상태, Adapter
- [`../Etc/RogueLikeInput.inputactions`](../Etc/RogueLikeInput.inputactions): 액션과 바인딩 원본
- [`../Pawn/Runtime/Controller/ManualPawnController.cs`](../Pawn/Runtime/Controller/ManualPawnController.cs): 현재 입력 상태를 읽는 컨트롤러
- [`../Pawn/Runtime/State/PawnIdleSprintState.cs`](../Pawn/Runtime/State/PawnIdleSprintState.cs): 입력을 이동 요청으로 변환하는 상태
- [`../Kinematic/Test/KinematicDynamicMoveTest.cs`](../Kinematic/Test/KinematicDynamicMoveTest.cs): 테스트 입력을 사용하는 컴포넌트
