using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    public sealed class KinematicBody
    {
        private readonly Transform target;
        private Vector2 anchorPosition;
        private Vector2 pendingMoveDelta;

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
            // 지금 텔레포트 요청 즉시 anchorPosition을 바꿔주는데 이러면 안된다
            // 텔레포트도 pending으로 넣어두고 solving 시점? 월드 업데이트 시점에 해주어야한다?
            // 왜냐면 같은 update 흐름에서 cast를 했을 때 그 업데이트에서의 위치는 아직 텔레포트하지 않은 위치인데 
            // cast에 검출되지 않을 수 있다.
            Debug.Assert(!IsStatic, $"Request Teleport to static body: {target.name}");
            anchorPosition = position;
            pendingMoveDelta = Vector2.zero;
        }

        internal Vector2 ConsumeDelta()
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
