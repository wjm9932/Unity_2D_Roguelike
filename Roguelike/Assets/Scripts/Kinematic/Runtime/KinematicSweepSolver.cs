using Kinematic.Data;
using Kinematic.Interface;
using Kinematic.Runtime.Spatial;
using log4net.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal sealed class KinematicSweepSolver : IKinematicMovementSolver
    {
        // 정적 바디 조회용 쿼드 트리
        private readonly IQueryOnlyQuadTree<KinematicBody> staticBodies;
        // 동적 바디 조회용 리스트
        private readonly IReadOnlyList<KinematicBody> dynamicBodies;
        // 쿼리 결과용 버퍼
        private readonly List<KinematicBody> queryResult = new();
        // 모든 dynamicBodies와 같은 순서로 상태를 생성하여 동일한 인덱스로 연결한다.
        private readonly List<SweepBodyState> sweepBodyStates = new();
        private readonly List<SweepContact> sweepContacts = new();

        private const int MaxSlideIterationCount = 4;
        private const float MinMoveDistance = 0.000001f;
        private const float SweepDirectionEpsilon = 0.000001f;

        private struct SweepBodyState
        {
            internal Vector2 RemainingMoveDelta { get; set; }
            internal int SlideIterationCount { get; set; }
            internal bool HasMoveDelta => RemainingMoveDelta.sqrMagnitude > MinMoveDistance * MinMoveDistance;

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
            internal bool SolveBodyA { get; }
            internal bool SolveBodyB { get; }

            internal SweepContact(int bodyAIndex, int bodyBIndex, Vector2 normal, bool solveBodyA, bool solveBodyB)
            {
                BodyAIndex = bodyAIndex;
                BodyBIndex = bodyBIndex;
                Normal = normal;
                SolveBodyA = solveBodyA;
                SolveBodyB = solveBodyB;
            }
        }

        internal KinematicSweepSolver(IQueryOnlyQuadTree<KinematicBody> statics, IReadOnlyList<KinematicBody> dynamics)
        {
            staticBodies = statics;
            dynamicBodies = dynamics;
        }

        public void Solve()
        {
            foreach (var body in dynamicBodies)
            {
                sweepBodyStates.Add(new SweepBodyState(body.ConsumeMoveDelta()));
            }

            Sweep();

            sweepBodyStates.Clear();
            sweepContacts.Clear();
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
                var moveDeltaA = stateA.HasMoveDelta ? stateA.RemainingMoveDelta : Vector2.zero;
                var sweptBoundsA = bodyA.Bounds.GetSweptBounds(moveDeltaA);

                // 남은 이동량이 없다면 정적 오브젝트와 충돌 검사를 하지 않는다.
                if (stateA.HasMoveDelta)
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
                    if (stateA.HasMoveDelta == false && stateB.HasMoveDelta == false)
                    {
                        continue;
                    }

                    var bodyB = dynamicBodies[bodyBIndex];
                    var moveDeltaB = stateB.HasMoveDelta ? stateB.RemainingMoveDelta : Vector2.zero;

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
            var solveBodyA = body.CanCollideWith(candidate) && sweepBodyStates[bodyAIndex].HasMoveDelta;
            var solveBodyB = bodyBIndex >= 0 && candidate.CanCollideWith(body) && sweepBodyStates[bodyBIndex].HasMoveDelta;

            if (!solveBodyA && !solveBodyB)
            {
                return;
            }

            if (KinematicShapeCast.TryCast(body.Center, body.Shape, candidate, moveDelta, out var candidateHit) == false)
            {
                return;
            }

            // 스침 또는 초기 겹침에서 빠져나가는 접촉은 가장 가까운 Hit을 선택하기 전에 제외한다.
            if (candidateHit.IsGrazing || Vector2.Dot(moveDelta, candidateHit.Normal) >= -SweepDirectionEpsilon || candidateHit.Fraction > closestFraction)
            {
                return;
            }

            // 자기 이동으로 슬라이드할 바디가 없다면 건너뛰고, 남은 겹침은 다음 프레임 MTV로 해소한다.
            solveBodyA = solveBodyA && Vector2.Dot(sweepBodyStates[bodyAIndex].RemainingMoveDelta, candidateHit.Normal) < 0f;
            solveBodyB = solveBodyB && Vector2.Dot(sweepBodyStates[bodyBIndex].RemainingMoveDelta, -candidateHit.Normal) < 0f;

            if (!solveBodyA && !solveBodyB)
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
            sweepContacts.Add(new SweepContact(bodyAIndex, bodyBIndex, candidateHit.Normal, solveBodyA, solveBodyB));
        }

        private void Sweep()
        {
            // 충돌 처리 횟수는 바디별로 제한하고, 다른 바디의 남은 이동은 계속 처리한다.
            while (HasRemainingMoveDelta())
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
                    if (contact.SolveBodyA)
                    {
                        SlideSweepBody(contact.BodyAIndex, contact.Normal);
                    }

                    if (contact.SolveBodyB)
                    {
                        // B에서 보는 상대 표면의 법선은 A가 사용하는 법선의 반대 방향이다.
                        SlideSweepBody(contact.BodyBIndex, -contact.Normal);
                    }
                }
            }
        }

        private bool HasRemainingMoveDelta()
        {
            foreach (var state in sweepBodyStates)
            {
                if (state.HasMoveDelta)
                {
                    return true;
                }
            }

            return false;
        }

        private void SlideSweepBody(int bodyIndex, Vector2 normal)
        {
            var state = sweepBodyStates[bodyIndex];
            if (!state.HasMoveDelta)
            {
                return;
            }

            // 남은 이동량을 hit.Normal 벡터에 투영한다.
            // 남은 이동량을 법선 방향으로 투영한다 = 남는 이동량(벡터) 중에 hit.Normal 방향 성분으로 이루어진 부분의 크기를 부호가 있는 스칼라 값으로 구한다. 이 부호를 가지는 크기를 통해 투영 벡터를 구할 수 있다.(hit.Normal 방향 or -hit.Normal 방향)
            var intoSurface = Vector2.Dot(state.RemainingMoveDelta, normal);
            if (intoSurface < 0f)
            {
                // 추가 슬라이드가 필요한 경우에만 한도를 확인하고 남은 이동을 포기한다.
                if (state.SlideIterationCount >= MaxSlideIterationCount)
                {
                    state.RemainingMoveDelta = Vector2.zero;
                    sweepBodyStates[bodyIndex] = state;
                    return;
                }

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
                if (state.HasMoveDelta == false)
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

        public void Dispose()
        {
        }
    }
}
