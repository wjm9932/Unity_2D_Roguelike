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
        private const float SweepDirectionEpsilon = 0.000001f;

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

        internal bool CircleCast(Vector2 origin, float radius, Vector2 distance, KinematicBody ignoredBody, out ShapeCastHit hit)
        {
            Debug.Assert(radius >= 0f, $"CircleCast radius must be non-negative: {radius}");

            if (radius < 0f)
            {
                hit = default;
                return false;
            }

            var shape = ColliderInfo.CreateCircle(Vector2.zero, radius);
            return ShapeCast(origin, shape, distance, ignoredBody, queryDynamicBodies: true, out hit);
        }

        internal bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 distance, KinematicBody ignoredBody, out ShapeCastHit hit)
        {
            Debug.Assert(halfExtents.x >= 0f && halfExtents.y >= 0f, $"BoxCast half extents must be non-negative: {halfExtents}");

            if (halfExtents.x < 0f || halfExtents.y < 0f)
            {
                hit = default;
                return false;
            }

            var shape = ColliderInfo.CreateBox(Vector2.zero, halfExtents);
            return ShapeCast(origin, shape, distance, ignoredBody, queryDynamicBodies: true, out hit);
        }

        internal bool ShapeCast(KinematicBody body, Vector2 distance, out ShapeCastHit hit) => ShapeCast(body.Center, body.Shape, distance, body, queryDynamicBodies: true, out hit);

        private bool ShapeCast(Vector2 origin, in ColliderInfo shape, Vector2 distance, KinematicBody ignoredBody, bool queryDynamicBodies, out ShapeCastHit hit)
        {
            hit = default;

            if (staticBodies == null || distance.sqrMagnitude <= Mathf.Epsilon)
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
            var sweptBounds = AABB.Create(origin, extents).GetSweptBounds(distance);

            queryResult.Clear();
            staticBodies.Query(sweptBounds, queryResult);

            foreach (var candidate in queryResult)
            {
                TryUpdateClosestHit(origin, shape, distance, ignoredBody, candidate, ref closestFraction, ref hit, ref hasHit);
            }

            queryResult.Clear();

            if (queryDynamicBodies == false)
            {
                return hasHit;
            }

            foreach (var candidate in dynamicBodies)
            {
                if (!candidate.Bounds.Overlaps(sweptBounds))
                {
                    continue;
                }

                TryUpdateClosestHit(origin, shape, distance, ignoredBody, candidate, ref closestFraction, ref hit, ref hasHit);
            }

            return hasHit;
        }

        private static void TryUpdateClosestHit(Vector2 origin, in ColliderInfo shape, Vector2 moveDelta, KinematicBody ignoredBody, KinematicBody candidate, ref float closestFraction, ref ShapeCastHit closestHit, ref bool hasHit)
        {
            if (candidate == ignoredBody || KinematicShapeCast.TryCast(origin, shape, candidate, moveDelta, out var candidateHit) == false || candidateHit.Fraction >= closestFraction)
            {
                return;
            }

            closestFraction = candidateHit.Fraction;
            closestHit = candidateHit;
            hasHit = true;
        }

        private bool SweepShapeCast(KinematicBody body, Vector2 moveDelta, out ShapeCastHit hit)
        {
            hit = default;

            if (staticBodies == null || moveDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            var hasHit = false;
            var closestFraction = float.PositiveInfinity;
            var sweptBounds = body.Bounds.GetSweptBounds(moveDelta);

            staticBodies.Query(sweptBounds, queryResult);

            foreach (var candidate in queryResult)
            {
                if (candidate == body || KinematicShapeCast.TryCast(body.Center, body.Shape, candidate, moveDelta, out var candidateHit) == false)
                {
                    continue;
                }

                // 스침 또는 초기 겹침에서 빠져나가는 접촉은 가장 가까운 Hit를 선택하기 전에 제외한다.
                if (candidateHit.IsGrazing || Vector2.Dot(moveDelta, candidateHit.Normal) >= -SweepDirectionEpsilon || candidateHit.Fraction >= closestFraction)
                {
                    continue;
                }

                closestFraction = candidateHit.Fraction;
                hit = candidateHit;
                hasHit = true;
            }

            return hasHit;
        }

        private void Sweep(KinematicBody body, Vector2 moveDelta)
        {
            var remainingMoveDelta = moveDelta;

            // 복잡한 코너에서 해결이 덜 됐으면 남은 움직임을 포기하고 안전한 위치에 멈추게
            for (var iteration = 0; iteration < MaxSlideIterationCount; iteration++)
            {
                var sqrDistance = remainingMoveDelta.sqrMagnitude;

                if (sqrDistance <= Mathf.Epsilon * Mathf.Epsilon)
                {
                    return;
                }

                var distance = remainingMoveDelta.magnitude;

                if (SweepShapeCast(body, remainingMoveDelta, out var hit) == false)
                {
                    body.ApplyMovement(remainingMoveDelta);
                    return;
                }

                // 부딪히지 않는 위치까지만 이동
                body.ApplyMovement(remainingMoveDelta * hit.Fraction);

                // 이동하고 남은 이동량 갱신
                remainingMoveDelta *= 1f - hit.Fraction;
                // 남은 이동량을 hit.Normal 벡터에 투영한다.
                // 남은 이동량을 법선 방향으로 투영한다 = 남는 이동량(벡터) 중에 hit.Normal 방향 성분으로 이루어진 부분의 크기를 부호가 있는 스칼라 값으로 구한다. 이 부호를 가지는 크기를 통해 투영 벡터를 구할 수 있다.(hit.Normal 방향 or -hit.Normal 방향)
                var intoSurface = Vector2.Dot(remainingMoveDelta, hit.Normal);
                if (intoSurface < 0f)
                {
                    // 내적 값이 음수이기 때문에 hit.Normal과 반대 방향인 투영 벡터가 만들어진다. 즉, 법선 반대 방향인 충돌 표면 안쪽을 향하는 벡터
                    // hit.Normal * intoSurface = 남은 이동량 중 법선 반대 방향인 충돌 표면 안쪽을 향하는 성분만 가진 벡터(intoSurface가 음수이기 때문) = 투영 벡터
                    // remainMoveDelta 성분에서 hit.Normal과 반대 방향 성분 즉, 충돌하려는 방향 성분을 제거한다.
                    remainingMoveDelta -= (hit.Normal * intoSurface);
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
                // Sweep에서 MaxSlideIterationCount를 통해 남은 이동량을 포기하더라도 안전한곳에서 멈추게 구현했기 때문
                ResolveInitialStaticOverlaps();
                // 일단 아직 dynamic vs static만 검사하는 방향
                // 추후 상대속도 이용해서 sweep도 dynamic vs dynamic 검사 구현할 예정
                SolveSweptMovements();
                // SolveSweptMovements 자체만으로는 처음부터 겹쳐있으면 Discrete한 MTV와 달리 겹침을 해소하지 않는다
                // Cast에서 Fraction = 0으로 겹쳐있다고 알려주지만 Fraction이 0이면 단순 움직이만 않을 뿐 겹침을 해소해 주지는 않는다
                // 그래서 처음부터 겹쳐있을 경우 해소하기 위해 ResolveInitialStaticOverlaps() 이거 호출해야함
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

        private void SolveSweptMovements()
        {
            foreach (var body in dynamicBodies)
            {
                Sweep(body, body.ConsumeDelta());
            }
        }

        private void SolveDiscreteMovements()
        {
            foreach (var body in dynamicBodies)
            {
                body.ApplyMovement(body.ConsumeDelta());
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
