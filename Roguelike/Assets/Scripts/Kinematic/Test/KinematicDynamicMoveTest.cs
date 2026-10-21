using System;
using Input;
using Kinematic.Data;
using R3;
using UnityEngine;

namespace Kinematic.Runtime
{
    [DisallowMultipleComponent]
    public sealed class KinematicDynamicMoveTest : MonoBehaviour
    {
        [SerializeField] private ColliderInfo colliderInfo = ColliderInfo.CreateCircle(Vector2.zero, 0.5f);
        [SerializeField] private AnimationCurve dashCurve;

        public float speed;

        public bool useInput = true;
        public MovePriority priority;

        private KinematicBody body;
        private IDisposable dashSubscription;
        private bool isDashing;
        private float dashElapsedTime;
        private float dashDuration;
        private float dashDistance;
        private Vector2 dashDirection;
        private Vector2 lastMoveDirection = Vector2.right;

        private void OnEnable()
        {
            body = new KinematicBody(transform, colliderInfo, priority);
            KinematicWorld.Register(body);
            dashSubscription = InputManager.Instance.PawnControls.Dash.Subscribe(value =>
            {
                if (value && useInput == true)
                {
                    Dash();
                    //Teleport();
                }
            });
        }

        private void Start()
        {
            //// 등록 순서 비교 실험: 모든 OnEnable 이후 이 세 바디만 A-B-C 순서로 다시 등록한다.
            //if (name != "Circle") return;

            //var tests = FindObjectsByType<KinematicDynamicMoveTest>(FindObjectsSortMode.None);
            //var orderedTests = new KinematicDynamicMoveTest[3];
            //foreach (var test in tests)
            //{
            //    if (test.gameObject.scene != gameObject.scene || test.isActiveAndEnabled == false || test.body == null)
            //    {
            //        continue;
            //    }

            //    switch (test.name)
            //    {
            //        case "Circle": orderedTests[0] = test; break;
            //        case "Circle (1)": orderedTests[1] = test; break;
            //        case "Circle (2)": orderedTests[2] = test; break;
            //    }
            //}

            //foreach (var test in orderedTests)
            //{
            //    if (test == null) return;
            //}

            //foreach (var test in orderedTests)
            //{
            //    KinematicWorld.Unregister(test.body);
            //}

            //foreach (var test in orderedTests)
            //{
            //    KinematicWorld.Register(test.body);
            //    Debug.Log($"[KinematicOrderTest] Registered {test.name}: priority={test.priority}, center={test.transform.position}");
            //}
        }

        private void Update()
        {
            if (useInput == false) return;

            if (isDashing && dashElapsedTime < dashDuration)
            {
                UpdateDash();
                // 마지막 대시 이동도 Ghost 상태로 Solve되도록 다음 Update에서 복구한다.
                return;
            }

            isDashing = false;
            body.SetGhost(false);

            var moveInput = GetMoveInput();
            if (moveInput.sqrMagnitude > Mathf.Epsilon)
                lastMoveDirection = moveInput.normalized;

            body.Move(moveInput * speed * Time.deltaTime);
        }

        private void Dash()
        {
            dashDuration = dashCurve[dashCurve.length - 1].time;
            var moveInput = GetMoveInput();
            dashDirection = moveInput.sqrMagnitude > Mathf.Epsilon ? moveInput.normalized : lastMoveDirection;
            lastMoveDirection = dashDirection;
            dashElapsedTime = 0f;
            dashDistance = 0f;
            isDashing = true;
            body.SetGhost(true);
        }

        private void Teleport()
        {
            if (useInput == false) return;

            body.Teleport(new Vector2(0f, 0f));
        }

        private void UpdateDash()
        {
            body.SetGhost(true);
            dashElapsedTime = Mathf.Min(dashElapsedTime + Time.deltaTime, dashDuration);

            var distance = dashCurve.Evaluate(dashElapsedTime);
            // 커브의 Y값은 누적 거리이므로 이전 샘플과의 차이만 이동한다.
            body.Move(dashDirection * (distance - dashDistance));
            dashDistance = distance;
        }

        private static Vector2 GetMoveInput() => InputManager.Instance.PawnControls.TestMove.CurrentValue.RawValue;

        private void OnDisable()
        {
            dashSubscription?.Dispose();
            dashSubscription = null;

            isDashing = false;
            body.SetGhost(false);
            KinematicWorld.Unregister(body);
            body = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var center = transform.position + (Vector3)colliderInfo.Offset;
            if (colliderInfo.Shape == Shape.Circle)
            {
                Gizmos.DrawWireSphere(center, colliderInfo.Radius);
            }
            else
            {
                Gizmos.DrawWireCube(center, new Vector3(colliderInfo.HalfExtents.x * 2f, colliderInfo.HalfExtents.y * 2f, 0f));
            }
        }
#endif
    }
}
