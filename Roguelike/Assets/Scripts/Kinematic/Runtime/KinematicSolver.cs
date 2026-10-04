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
        private readonly List<Vector2> discreteMoveDeltas = new();
        private readonly List<SweepBodyState> sweepBodyStates = new();
        private readonly List<SweepContact> sweepContacts = new();
        private QuadTree staticBodies;
        private bool useSweep;

        private const int MaxSolverIterationCount = 16;
        private const int MaxSlideIterationCount = 4;
        private const float MinMoveDistance = 0.000001f;
        private const float SweepDirectionEpsilon = 0.000001f;

        private struct SweepBodyState
        {
            internal Vector2 RemainingMoveDelta { get; set;  }
            internal int SlideIterationCount { get; set; }
            internal bool IsMovementComplete => RemainingMoveDelta.sqrMagnitude <= MinMoveDistance * MinMoveDistance;

            internal SweepBodyState(Vector2 moveDelta)
            {
                RemainingMoveDelta = moveDelta;
                SlideIterationCount = 0;
            }
        }

        private readonly struct SweepContact
        {
            internal int BodyAIndex { get; }
            internal int BodyBIndex { get; }
            internal Vector2 Normal { get; }

            internal SweepContact(int bodyAIndex, int bodyBIndex, Vector2 normal)
            {
                BodyAIndex = bodyAIndex;
                BodyBIndex = bodyBIndex;
                Normal = normal;
            }
        }

#if UNITY_EDITOR
        internal void DrawQuadTreeBounds() => staticBodies?.DrawBounds(dynamicBodies);
#endif

        internal bool Init(AABB bounds, bool useSweep)
        {
            staticBodies = new QuadTree(bounds);
            this.useSweep = useSweep;

            return true;
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

        private bool SweepShapeCast(out float closestFraction)
        {
            sweepContacts.Clear();
            closestFraction = float.PositiveInfinity;

            if (staticBodies == null)
            {
                return false;
            }

            for (var bodyAIndex = 0; bodyAIndex < dynamicBodies.Count; bodyAIndex++)
            {
                var bodyA = dynamicBodies[bodyAIndex];
                var stateA = sweepBodyStates[bodyAIndex];
                var moveDeltaA = stateA.IsMovementComplete ? Vector2.zero : stateA.RemainingMoveDelta;
                var sweptBoundsA = bodyA.Bounds.GetSweptBounds(moveDeltaA);

                // 남은 이동량이 없다면 정적 오브젝트와 충돌 검사를 하지 않는다.
                if (!stateA.IsMovementComplete)
                {
                    staticBodies.Query(sweptBoundsA, queryResult);

                    foreach (var candidate in queryResult)
                    {
                        TryUpdateClosestSweepHit(bodyAIndex, -1, candidate, moveDeltaA, ref closestFraction);
                    }
                }

                for (var bodyBIndex = bodyAIndex + 1; bodyBIndex < dynamicBodies.Count; bodyBIndex++)
                {
                    var stateB = sweepBodyStates[bodyBIndex];
                    // 오브젝트 둘 다 남은 이동량이 없다면 건너 뛴다.
                    if (stateA.IsMovementComplete && stateB.IsMovementComplete)
                    {
                        continue;
                    }

                    var bodyB = dynamicBodies[bodyBIndex];
                    var moveDeltaB = stateB.IsMovementComplete ? Vector2.zero : stateB.RemainingMoveDelta;

                    // 양쪽의 이동 경로를 비교해야 정지한 A 쪽으로 이동하는 B도 검출된다.
                    if (!sweptBoundsA.Overlaps(bodyB.Bounds.GetSweptBounds(moveDeltaB)))
                    {
                        continue;
                    }

                    // bodyA의 상대 속도
                    var relativeMoveDelta = moveDeltaA - moveDeltaB;
                    TryUpdateClosestSweepHit(bodyAIndex, bodyBIndex, bodyB, relativeMoveDelta, ref closestFraction);
                }
            }

            return sweepContacts.Count > 0;
        }

        private void TryUpdateClosestSweepHit(int bodyAIndex, int bodyBIndex, KinematicBody candidate, Vector2 moveDelta, ref float closestFraction)
        {
            var body = dynamicBodies[bodyAIndex];
            if (KinematicShapeCast.TryCast(body.Center, body.Shape, candidate, moveDelta, out var candidateHit) == false)
            {
                return;
            }

            // 스침 또는 초기 겹침에서 빠져나가는 접촉은 가장 가까운 Hit을 선택하기 전에 제외한다.
            if (candidateHit.IsGrazing || Vector2.Dot(moveDelta, candidateHit.Normal) >= -SweepDirectionEpsilon || candidateHit.Fraction > closestFraction)
            {
                return;
            }

            if (candidateHit.Fraction < closestFraction)
            {
                closestFraction = candidateHit.Fraction;
                sweepContacts.Clear();
            }

            // 동적 쌍은 상대 좌표계로 cast하므로 월드 접촉점 대신 법선만 저장한다.
            // 한 프레임에 같이 처리하기 위해 같은 충돌 시점의 접촉을 모두 저장한다.
            sweepContacts.Add(new SweepContact(bodyAIndex, bodyBIndex, candidateHit.Normal));
        }

        private void Sweep()
        {
            // 충돌 처리 횟수는 바디별로 제한하고, 다른 바디의 남은 이동은 계속 처리한다.
            while (true)
            {
                if (SweepShapeCast(out var fraction) == false)
                {
                    ApplySweptMovements(1f);
                    return;
                }

                // 모든 바디를 바디들 중 가장 이른 충돌 시점까지 각자의 이동 경로를 따라 이동시킨다.
                ApplySweptMovements(fraction);

                foreach (var contact in sweepContacts)
                {
                    SlideSweepBody(contact.BodyAIndex, contact.Normal);

                    if (contact.BodyBIndex >= 0)
                    {
                        // B에서 보는 상대 표면의 법선은 A가 사용하는 법선의 반대 방향이다.
                        SlideSweepBody(contact.BodyBIndex, -contact.Normal);
                    }
                }
            }
        }

        private void SlideSweepBody(int bodyIndex, Vector2 normal)
        {
            var state = sweepBodyStates[bodyIndex];
            if (state.IsMovementComplete)
            {
                return;
            }

            // 한도에 도달한 바디는 표면 밖으로 이동하더라도 일괄적으로 남은 이동을 포기한다.
            if (state.SlideIterationCount >= MaxSlideIterationCount)
            {
                state.RemainingMoveDelta = Vector2.zero;
                sweepBodyStates[bodyIndex] = state;
                return;
            }

            // 남은 이동량을 hit.Normal 벡터에 투영한다.
            // 남은 이동량을 법선 방향으로 투영한다 = 남는 이동량(벡터) 중에 hit.Normal 방향 성분으로 이루어진 부분의 크기를 부호가 있는 스칼라 값으로 구한다. 이 부호를 가지는 크기를 통해 투영 벡터를 구할 수 있다.(hit.Normal 방향 or -hit.Normal 방향)
            var intoSurface = Vector2.Dot(state.RemainingMoveDelta, normal);
            if (intoSurface < 0f)
            {
                state.SlideIterationCount++;
                // 내적 값이 음수이기 때문에 hit.Normal과 반대 방향인 투영 벡터가 만들어진다. 즉, 법선 반대 방향인 충돌 표면 안쪽을 향하는 벡터
                // hit.Normal * intoSurface = 남은 이동량 중 법선 반대 방향인 충돌 표면 안쪽을 향하는 성분만 가진 벡터(intoSurface가 음수이기 때문) = 투영 벡터
                // remainMoveDelta 성분에서 hit.Normal과 반대 방향 성분 즉, 충돌하려는 방향 성분을 제거한다.
                state.RemainingMoveDelta -= normal * intoSurface;
                sweepBodyStates[bodyIndex] = state;
            }
        }

        private void ApplySweptMovements(float fraction)
        {
            for (var bodyIndex = 0; bodyIndex < dynamicBodies.Count; bodyIndex++)
            {
                var state = sweepBodyStates[bodyIndex];
                if (state.IsMovementComplete)
                {
                    continue;
                }

                // 부딪히지 않는 위치까지만 이동
                dynamicBodies[bodyIndex].ApplyMovement(state.RemainingMoveDelta * fraction);
                // 이동하고 남은 이동량 갱신
                state.RemainingMoveDelta *= (1f - fraction);
                // 상태 갱신
                sweepBodyStates[bodyIndex] = state;
            }
        }

        // 텔레포트도 여기서 해주는 방향으로 해야할 듯
        // 일단 sweep 먼저 구현하고 나중에 생각하자
        internal void Solve()
        {
            if (useSweep)
            {
                // Fraction = 0인 cast는 기존 겹침을 해소하지 않으므로 먼저 MTV로 분리한다.
                ResolveDiscreteCollisions();
                SolveSweptMovements();
            }
            else
            {
                // 기존 겹침은 이동으로 생긴 충돌과 구분해서 먼저 분리한다.
                ResolveDiscreteCollisions();
                SolveDiscreteMovements();
                ResolveDiscreteCollisions(useMovementDeltas: true);
                discreteMoveDeltas.Clear();
            }

            SyncDynamicTransforms();
        }

        private void SolveSweptMovements()
        {
            foreach (var body in dynamicBodies)
            {
                sweepBodyStates.Add(new SweepBodyState(body.ConsumeDelta()));
            }

            Sweep();

            sweepBodyStates.Clear();
            sweepContacts.Clear();
        }

        private void SolveDiscreteMovements()
        {
            foreach (var body in dynamicBodies)
            {
                var moveDelta = body.ConsumeDelta();
                discreteMoveDeltas.Add(moveDelta);
                body.ApplyMovement(moveDelta);
            }
        }

        private void ResolveDiscreteCollisions(bool useMovementDeltas = false)
        {
            for (var iteration = 0; iteration < MaxSolverIterationCount; iteration++)
            {
                var hasDynamicStaticCollision = SolveDynamicStaticCollisions(useMovementDeltas);
                var hasDynamicDynamicCollision = SolveDynamicDynamicCollisions(useMovementDeltas);

                if (!hasDynamicStaticCollision && !hasDynamicDynamicCollision)
                {
                    break;
                }
            }
        }

        private bool SolveDynamicStaticCollisions(bool useMovementDeltas)
        {
            var hasCollision = false;

            for (var bodyIndex = 0; bodyIndex < dynamicBodies.Count; bodyIndex++)
            {
                var dynamicBody = dynamicBodies[bodyIndex];
                staticBodies.Query(dynamicBody.Bounds, queryResult);

                foreach (var staticBody in queryResult)
                {
                    if (!dynamicBody.TryCollide(staticBody, out var contact))
                    {
                        continue;
                    }

                    dynamicBody.ApplyCorrection(contact.SeparationMtv);
                    if (useMovementDeltas)
                    {
                        discreteMoveDeltas[bodyIndex] += contact.SeparationMtv;
                    }
                    hasCollision = true;
                }
            }

            return hasCollision;
        }

        private bool SolveDynamicDynamicCollisions(bool useMovementDeltas)
        {
            var hasCollision = false;

            for (var bodyAIndex = 0; bodyAIndex < dynamicBodies.Count; bodyAIndex++)
            {
                var bodyA = dynamicBodies[bodyAIndex];

                for (var bodyBIndex = bodyAIndex + 1; bodyBIndex < dynamicBodies.Count; bodyBIndex++)
                {
                    var bodyB = dynamicBodies[bodyBIndex];

                    // 충돌 검사
                    if (!bodyA.TryCollide(bodyB, out var contact))
                    {
                        continue;
                    }

                    if (!useMovementDeltas)
                    {
                        var halfMtv = contact.SeparationMtv * 0.5f;
                        bodyA.ApplyCorrection(halfMtv);
                        bodyB.ApplyCorrection(-halfMtv);
                    }
                    // 서로 지나쳐 접촉 법선이 뒤집히면 접근한 바디 대신 상대 바디가 보정될 수 있다.
                    else
                    {
                        var moveDeltaA = discreteMoveDeltas[bodyAIndex];
                        var moveDeltaB = discreteMoveDeltas[bodyBIndex];
                        var normal = contact.SeparationNormal;

                        // 바디 A의 법선 안쪽 이동량
                        var correctionWeightA = Mathf.Max(0f, Vector2.Dot(moveDeltaA, -normal));
                        // 바디 B의 법선 안쪽 이동량
                        var correctionWeightB = Mathf.Max(0f, Vector2.Dot(moveDeltaB, normal));
                        // 두 바디의 법선 안쪽 이동량 합. 충돌 전 간격을 좁힌 이동도 포함되므로 겹침 크기와 다를 수 있다.
                        var totalWeight = correctionWeightA + correctionWeightB;
                        // 이동 후 법선에 안쪽 이동 성분이 없는 경우
                        if (totalWeight <= MinMoveDistance)
                        {
                            // 이동량 크기로 보정을 분담하여 정지한 바디는 밀지 않는다.
                            correctionWeightA = moveDeltaA.magnitude;
                            correctionWeightB = moveDeltaB.magnitude;
                            totalWeight = correctionWeightA + correctionWeightB;
                        }

                        if (totalWeight <= MinMoveDistance)
                        {
                            continue;
                        }

                        // 각 바디의 가중치 비율에 따라 MTV를 분담한다.
                        // A는 법선 방향으로, B는 반대 방향으로 보정한다.
                        var correctionA = contact.SeparationMtv * (correctionWeightA / totalWeight);
                        var correctionB = -contact.SeparationMtv * (correctionWeightB / totalWeight);

                        // 보정 적용
                        bodyA.ApplyCorrection(correctionA);
                        bodyB.ApplyCorrection(correctionB);

                        discreteMoveDeltas[bodyAIndex] += correctionA;
                        discreteMoveDeltas[bodyBIndex] += correctionB;
                    }

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

        internal void Dispose()
        {

        }
    }
}
