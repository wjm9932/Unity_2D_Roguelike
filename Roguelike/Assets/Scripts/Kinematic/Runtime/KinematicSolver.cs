using Kinematic.Data;
using Kinematic.Runtime.Spatial;
using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal sealed class KinematicSolver
    {
        internal static KinematicSolver Instance { get; } = new();

        private readonly List<KinematicBody> dynamicBodies = new();
        private readonly List<KinematicBody> queryResult = new();
        private QuadTree staticBodies;
        private bool useSweep;

        private const int MaxSolverIterationCount = 16;
        private const int MaxSlideIterationCount = 4;
        private const float SkinWidth = 0.001f;

        private KinematicSolver()
        {
        }

#if UNITY_EDITOR
        internal void DrawQuadTreeBounds() => staticBodies?.DrawBounds(dynamicBodies);
#endif

        internal void Init(AABB bounds, bool useSweep)
        {
            staticBodies = new QuadTree(bounds);
            this.useSweep = useSweep;
        }

        internal void Register(KinematicBody body)
        {
            if (body.IsStatic)
            {
                staticBodies.Insert(body);
            }
            else
            {
                dynamicBodies.Add(body);
            }
        }

        internal void Unregister(KinematicBody body)
        {
            if (body.IsStatic)
            {
                staticBodies.Remove(body);
            }
            else
            {
                dynamicBodies.Remove(body);
            }
        }

        internal bool CircleCast(Vector2 origin, float radius, Vector2 moveDelta, out ShapeCastHit hit)
        {
            Debug.Assert(radius >= 0f, $"CircleCast radius must be non-negative: {radius}");

            if (radius < 0f)
            {
                hit = default;
                return false;
            }

            var shape = ColliderInfo.CreateCircle(Vector2.zero, radius);
            return ShapeCast(origin, shape, moveDelta, null, includeDynamicBodies: true, out hit);
        }

        internal bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 moveDelta, out ShapeCastHit hit)
        {
            Debug.Assert(halfExtents.x >= 0f && halfExtents.y >= 0f, $"BoxCast half extents must be non-negative: {halfExtents}");

            if (halfExtents.x < 0f || halfExtents.y < 0f)
            {
                hit = default;
                return false;
            }

            var shape = ColliderInfo.CreateBox(Vector2.zero, halfExtents);
            return ShapeCast(origin, shape, moveDelta, null, includeDynamicBodies: true, out hit);
        }

        internal bool ShapeCast(KinematicBody body, Vector2 moveDelta, out ShapeCastHit hit) => ShapeCast(body.Center, body.Shape, moveDelta, body, includeDynamicBodies: false, out hit);

        private bool ShapeCast(Vector2 origin, in ColliderInfo shape, Vector2 moveDelta, KinematicBody ignoredBody, bool includeDynamicBodies, out ShapeCastHit hit)
        {
            hit = default;

            if (staticBodies == null || moveDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            var hasHit = false;
            var closestFraction = float.PositiveInfinity;
            var extents = shape.Shape switch
            {
                Shape.Circle => Vector2.one * shape.Radius,
                Shape.Box => shape.HalfExtents,
                _ => Vector2.zero
            };
            // 쿼드 트리 조회용 이동경로 AABB
            var sweptBounds = AABB.Create(origin, extents).GetSweptBounds(moveDelta);

            queryResult.Clear();
            staticBodies.Query(sweptBounds, queryResult);

            foreach (var candidate in queryResult)
            {
                TryUpdateClosestHit(origin, shape, moveDelta, ignoredBody, candidate, ref closestFraction, ref hit, ref hasHit);
            }

            queryResult.Clear();

            if (!includeDynamicBodies)
            {
                return hasHit;
            }

            foreach (var candidate in dynamicBodies)
            {
                if (!candidate.Bounds.Overlaps(sweptBounds))
                {
                    continue;
                }

                TryUpdateClosestHit(origin, shape, moveDelta, ignoredBody, candidate, ref closestFraction, ref hit, ref hasHit);
            }

            return hasHit;
        }

        private static void TryUpdateClosestHit(Vector2 origin, in ColliderInfo shape, Vector2 moveDelta, KinematicBody ignoredBody, KinematicBody candidate, ref float closestFraction, ref ShapeCastHit closestHit, ref bool hasHit)
        {
            if (candidate == ignoredBody || !KinematicShapeCast.TryCast(origin, shape, candidate, moveDelta, out var candidateHit) || candidateHit.Fraction >= closestFraction)
            {
                return;
            }

            closestFraction = candidateHit.Fraction;
            closestHit = candidateHit;
            hasHit = true;
        }

        private void MoveAndSlide(KinematicBody body, Vector2 moveDelta)
        {
            var remainingMoveDelta = moveDelta;

            // 복잡한 코너에서 해결이 덜 됐으면 남은 움직임을 포기하고 안전한 위치에 멈추게 
            for (var iteration = 0; iteration < MaxSlideIterationCount; iteration++)
            {
                var distance = remainingMoveDelta.magnitude;

                if (distance <= Mathf.Epsilon)
                {
                    return;
                }

                if (!ShapeCast(body, remainingMoveDelta, out var hit))
                {
                    body.ApplyMovement(remainingMoveDelta);
                    return;
                }

                var safeDistance = Mathf.Max(0f, hit.Distance - SkinWidth);
                body.ApplyMovement(remainingMoveDelta * (safeDistance / distance));

                remainingMoveDelta *= 1f - hit.Fraction;
                var intoSurface = Vector2.Dot(remainingMoveDelta, hit.Normal);

                if (intoSurface < 0f)
                {
                    remainingMoveDelta -= hit.Normal * intoSurface;
                }
            }
        }

        // 텔레포트도 여기서 해주는 방향으로 해야할 듯
        // 일단 sweep 먼저 구현하고 나중에 생각하자
        internal void Solve()
        {
            if (useSweep)
           {
                // 혹시 모를 Teleport로 인한 또는 sweep 시작전 충돌 상태 solving
                // dynamic이 teleport로 dynamic과 충돌 상태일 수 있는데 이 경우는 지금은 일단 무시

                // 일반적인 sweep만 수행했을 때는 겹쳐져있는 상태로 solving이 끝날 수 없음
                // MoveAndSlide에서 MaxSlideIterationCount를 통해 남은 이동량을 포기하더라도 안전한곳에서 멈추게 구현했기 때문
                ResolveInitialStaticOverlaps();
                // 일단 아직 dynamic vs static만 검사하는 방향
                // 추후 상대속도 이용해서 sweep도 dynamic vs dynamic 검사 구현할 예정
                SolveSweptMovements();
            }
            else
            {
                SolveDiscreteMovements();
                ResolveDiscreteCollisions();
            }

            SyncDynamicTransforms();
        }

        private void ResolveInitialStaticOverlaps()
        {
            for (var iteration = 0; iteration < MaxSolverIterationCount; iteration++)
            {
                if (!SolveDynamicStaticCollisions())
                {
                    break;
                }
            }
        }

        private void ResolveDiscreteCollisions()
        {
            for (var iteration = 0; iteration < MaxSolverIterationCount; iteration++)
            {
                var hasDynamicStaticCollision = SolveDynamicStaticCollisions();
                var hasDynamicDynamicCollision = SolveDynamicDynamicCollisions();

                if (!hasDynamicStaticCollision && !hasDynamicDynamicCollision)
                {
                    break;
                }
            }
        }

        private void SolveDiscreteMovements()
        {
            foreach (var body in dynamicBodies)
            {
                body.ApplyMovement(body.ConsumeMoveDelta());
            }
        }

        private void SolveSweptMovements()
        {
            foreach (var body in dynamicBodies)
            {
                MoveAndSlide(body, body.ConsumeMoveDelta());
            }
        }

        private bool SolveDynamicStaticCollisions()
        {
            var hasCollision = false;

            foreach (var dynamicBody in dynamicBodies)
            {
                staticBodies.Query(dynamicBody.Bounds, queryResult);

                foreach (var staticBody in queryResult)
                {
                    if (!dynamicBody.TryCollide(staticBody, out var contact))
                    {
                        continue;
                    }

                    dynamicBody.ApplyCorrection(contact.SeparationMtv);
                    hasCollision = true;
                }

                queryResult.Clear();
            }

            return hasCollision;
        }

        private bool SolveDynamicDynamicCollisions()
        {
            var hasCollision = false;

            for (var bodyAIndex = 0; bodyAIndex < dynamicBodies.Count; bodyAIndex++)
            {
                var bodyA = dynamicBodies[bodyAIndex];

                for (var bodyBIndex = bodyAIndex + 1; bodyBIndex < dynamicBodies.Count; bodyBIndex++)
                {
                    var bodyB = dynamicBodies[bodyBIndex];

                    if (!bodyA.TryCollide(bodyB, out var contact))
                    {
                        continue;
                    }

                    var halfMtv = contact.SeparationMtv * 0.5f;

                    bodyA.ApplyCorrection(halfMtv);
                    bodyB.ApplyCorrection(-halfMtv);
                    hasCollision = true;
                }
            }

            return hasCollision;
        }

        private void SyncDynamicTransforms()
        {
            foreach (var body in dynamicBodies)
            {
                body.SyncTransform();
            }
        }
    }
}
