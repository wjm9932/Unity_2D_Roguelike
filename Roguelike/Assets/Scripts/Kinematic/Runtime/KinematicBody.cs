using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    /// <summary>
    /// KinematicBody는 특정 프로젝트에 종속되지 않는 독립적인 바디 기능을 제공한다.
    /// Move, Teleport 등 공개 API는 사용하는 프로젝트와 관계없이 일관된 동작 규칙을 유지한다.
    /// 프로젝트별로 허용할 기능과 접근 범위를 제한하는 것은 인게임 로직의 책임이다.
    /// 이러한 제한은 KinematicBody를 수정하지 않고, 필요하다면 인게임 로직의 별도 래퍼로 구현한다.
    /// </summary>
    public sealed class KinematicBody
    {
        private readonly Transform target;
        private Vector2 anchorPosition;
        private Vector2 pendingMoveDelta;
        private Vector2? pendingTeleportPosition;
        
        #region Smooth Correction
        private Vector2 smoothCorrectionOffset;
        private Vector2 smoothCorrectionStartOffset;
        private float smoothCorrectionElapsed;
        private bool isSmoothingCorrection;
        private const float InverseSmoothCorrectionDuration = 1f / 0.1f;
        #endregion

#if UNITY_EDITOR
        internal string Name { get; }

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
        public int Layer => Shape.Layer;
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
        internal MovePriority Priority { get; }
        internal bool IsStatic { get; }
        internal bool IsTrigger { get; private set; }
        internal bool NeedsGhostRecovery { get; set; }

        public KinematicBody(Transform transform, ColliderInfo shape, MovePriority movePriority, bool isTrigger)
        {
            target = transform;
            anchorPosition = transform.position;
            Shape = shape;
            Priority = movePriority;
            IsTrigger = isTrigger;
            IsStatic = false;

#if UNITY_EDITOR
            Name = transform.name;
#endif
        }

        public KinematicBody(Transform transform, ColliderInfo shape, bool isTrigger)
        {
            target = transform;
            anchorPosition = transform.position;
            Shape = shape;
            IsTrigger = isTrigger;
            IsStatic = true;

#if UNITY_EDITOR
            Name = transform.name;
#endif
        }

        public void SetGhost(bool isGhost)
        {
            if (IsTrigger == isGhost) return;

            // 고스트가 끝나면 종료한 바디만 기존 Pawn 겹침을 해소한다.
            NeedsGhostRecovery = isGhost == false;
            IsTrigger = isGhost;
        }

        public void Teleport(Vector2 position)
        {
            Debug.Assert(IsStatic == false, $"Request Teleport to static body: {target.name}");

            // 텔레포트 이동도 solve 시점과 통일하기 위해서 즉시 적용하지 않는다.
            // Teleport와 Cast의 호출 순서가 조회 결과에 영향을 주지 않도록 Solve 시점에 일반 이동과 함께 일괄 적용한다.
            pendingTeleportPosition = position;
        }

        public void Move(Vector2 moveDelta)
        {
            Debug.Assert(IsStatic == false, $"Request Move to static body: {target.name}");

            pendingMoveDelta += moveDelta;
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

        internal void ApplySmoothCorrection(Vector2 mtv)
        {
            smoothCorrectionOffset -= mtv;
            smoothCorrectionStartOffset = smoothCorrectionOffset;
            smoothCorrectionElapsed = 0f;
            isSmoothingCorrection = true;

            ApplyCorrection(mtv);
        }

        internal void ApplyMovement(Vector2 delta) => anchorPosition += delta;

        internal void SyncTransform()
        {
            if (isSmoothingCorrection)
            {
                smoothCorrectionElapsed += Time.deltaTime;
                var t = Mathf.Clamp01(smoothCorrectionElapsed * InverseSmoothCorrectionDuration);
                var easeoutProgress = t * (2f - t);
                smoothCorrectionOffset = smoothCorrectionStartOffset * (1f - easeoutProgress);

                if (t >= 1f)
                {
                    isSmoothingCorrection = false;
                    smoothCorrectionOffset = Vector2.zero;
                    smoothCorrectionStartOffset = Vector2.zero;
                }
            }

            target.position = anchorPosition + smoothCorrectionOffset;

#if UNITY_EDITOR
            DrawMovementDebug();
#endif
        }
    }
}
