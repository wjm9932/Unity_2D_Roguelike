using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    public sealed class KinematicBody
    {
        private readonly Transform target;
        private Vector2 anchorPosition;
        private Vector2 pendingMoveDelta;
        private Vector2? pendingTeleportPosition;

#if UNITY_EDITOR
        private Vector2 debugStartPosition;
        private Vector2 debugRequestedMoveDelta;

        private void DrawMovementDebug()
        {
            var center = (Vector3)Center;
            DrawMovementDirection(center, debugRequestedMoveDelta, Color.yellow);
            DrawMovementDirection(center, anchorPosition - debugStartPosition, Color.cyan);
        }

        private static void DrawMovementDirection(Vector3 center, Vector2 moveDelta, Color color)
        {
            var distance = moveDelta.magnitude;
            if (distance <= Mathf.Epsilon) return;

            var direction = moveDelta / distance;
            Debug.DrawLine(center, center + (Vector3)(direction * 2f), color, 0f, false);
        }
#endif

        public ColliderInfo Shape { get; }
        public Vector2 Center => anchorPosition + Shape.Offset;
        internal bool IsStatic { get; }
        internal AABB Bounds
        {
            get
            {
                var extents = Shape.Shape switch
                {
                    Data.Shape.Circle => Vector2.one * Shape.Radius,
                    Data.Shape.Box => Shape.HalfExtents,
                    _ => Vector2.zero
                };

                return AABB.Create(Center, extents);
            }
        }

        public KinematicBody(Transform transform, ColliderInfo shape, bool isStatic)
        {
            target = transform;
            anchorPosition = transform.position;
            Shape = shape;
            IsStatic = isStatic;
        }

        public void Move(Vector2 moveDelta)
        {
            Debug.Assert(!IsStatic, $"Request Move to static body: {target.name}");

            pendingMoveDelta += moveDelta;
        }

        public void Teleport(Vector2 position)
        {
            Debug.Assert(!IsStatic, $"Request Teleport to static body: {target.name}");

            // 텔레포트 이동도 solve 시점과 통일하기 위해서 즉시 적용하지 않는다.
            // Teleport와 Cast의 호출 순서가 조회 결과에 영향을 주지 않도록 Solve 시점에 일반 이동과 함께 일괄 적용한다.
            pendingTeleportPosition = position;
        }

        internal void ApplyTeleport()
        {
            if (pendingTeleportPosition.HasValue == false) return;

            anchorPosition = pendingTeleportPosition.Value;
            pendingTeleportPosition = null;
        }

        internal Vector2 ConsumeMoveDelta()
        {
#if UNITY_EDITOR
            debugStartPosition = anchorPosition;
            debugRequestedMoveDelta = pendingMoveDelta;
#endif
            var moveDelta = pendingMoveDelta;
            pendingMoveDelta = Vector2.zero;
            return moveDelta;
        }

        internal void ApplyCorrection(Vector2 mtv) => anchorPosition += mtv;

        internal void ApplyMovement(Vector2 delta) => anchorPosition += delta;

        internal void SyncTransform()
        {
            target.position = anchorPosition;

#if UNITY_EDITOR
            DrawMovementDebug();
#endif
        }
    }
}
