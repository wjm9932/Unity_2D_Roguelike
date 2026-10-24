using Kinematic.Interface;
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
        private readonly List<Vector2> contactNormals = new();
        private readonly List<SweepBodyState> simultaneousSlideStates = new();
        private const int MaxSlideIterationCount = 1;
        private const float MinMoveDistance = 0.000001f;
        private const float SweepDirectionEpsilon = 0.000001f;

        private struct SweepBodyState
        {
            internal Vector2 RemainingMoveDelta { get; set; }
            internal Vector2 StartCenter { get; }
            internal Vector2 AppliedMoveDelta { get; set; }
            internal int SlideIterationCount { get; set; }
            internal int PushPriority { get; set; }
            internal bool HasMoveDelta => RemainingMoveDelta.sqrMagnitude > MinMoveDistance * MinMoveDistance;

            internal SweepBodyState(Vector2 moveDelta, int pushPriority, Vector2 startCenter)
            {
                RemainingMoveDelta = moveDelta;
                StartCenter = startCenter;
                AppliedMoveDelta = Vector2.zero;
                SlideIterationCount = 0;
                PushPriority = pushPriority;
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

        internal KinematicSweepSolver(IQueryOnlyQuadTree<KinematicBody> statics, IReadOnlyList<KinematicBody> dynamics)
        {
            staticBodies = statics;
            dynamicBodies = dynamics;
        }

        public void Solve()
        {
            for (var bodyIndex = 0; bodyIndex < dynamicBodies.Count; bodyIndex++)
            {
                var body = dynamicBodies[bodyIndex];
                sweepBodyStates.Add(new SweepBodyState(body.ConsumeMoveDelta(), (int)body.Priority, body.Center));
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
                    if (sweptBoundsA.Overlaps(bodyB.Bounds.GetSweptBounds(moveDeltaB)) == false)
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
            if (body.CanCollideWith(candidate) == false)
            {
                return;
            }

            if (KinematicCast.TryCast(body.Center, body.Shape, candidate, moveDelta, out var candidateHit) == false)
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
            // 한 프레임에 같이 처리하기 위해 Clear하지 않고 같은 충돌 시점의 접촉을 모두 저장한다.
            sweepContacts.Add(new SweepContact(bodyAIndex, bodyBIndex, candidateHit.Normal));
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

                SlideAgainstStationaryContacts();

                foreach (var contact in sweepContacts)
                {
                    // 벽과 부딪혔을 경우
                    if (contact.BodyBIndex < 0)
                    {
                        SlideSweepBody(contact.BodyAIndex, contact.Normal);
                    }
                    else
                    {
                        var priorityA = sweepBodyStates[contact.BodyAIndex].PushPriority;
                        var priorityB = sweepBodyStates[contact.BodyBIndex].PushPriority;
                        if (priorityA == priorityB)
                        {
                            Debug.Log(priorityA);
                            SlideEqualPriorityBodies(contact.BodyAIndex, contact.BodyBIndex, contact.Normal);
                        }
                        else if (priorityA < priorityB)
                        {
                            SlideLowerPriorityBody(contact.BodyAIndex, contact.BodyBIndex, contact.Normal);
                        }
                        else
                        {
                            SlideLowerPriorityBody(contact.BodyBIndex, contact.BodyAIndex, -contact.Normal);
                        }
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

        private void SlideAgainstStationaryContacts()
        {
            // 동시 충돌시에만 순서에 따라 이동량이 변할 수 있기에 동시에 2개 이상 충돌 했을 때만 검사한다.
            if (sweepContacts.Count < 2)
            {
                return;
            }

            for (var bodyIndex = 0; bodyIndex < sweepBodyStates.Count; bodyIndex++)
            {
                var state = sweepBodyStates[bodyIndex];

                contactNormals.Clear();

                if (state.HasMoveDelta)
                {
                    foreach (var contact in sweepContacts)
                    {
                        if (contact.BodyAIndex == bodyIndex)
                        {
                            AddStationaryContactNormal(state, contact.BodyBIndex, contact.Normal);
                        }
                        else if (contact.BodyBIndex == bodyIndex)
                        {
                            AddStationaryContactNormal(state, contact.BodyAIndex, -contact.Normal);
                        }
                    }
                }

                if (contactNormals.Count >= 2 && IsMovingInto(state.RemainingMoveDelta))
                {
                    state.RemainingMoveDelta = state.SlideIterationCount >= MaxSlideIterationCount ? Vector2.zero : FindClosestNonPenetratingMoveDelta(state.RemainingMoveDelta);
                    state.SlideIterationCount++;
                }

                // 다른 바디가 먼저 보정되면서 정지 여부나 우선순위 판정이 바뀌지 않도록 함께 반영한다.
                simultaneousSlideStates.Add(state);
            }

            for (var bodyIndex = 0; bodyIndex < sweepBodyStates.Count; bodyIndex++)
            {
                sweepBodyStates[bodyIndex] = simultaneousSlideStates[bodyIndex];
            }

            simultaneousSlideStates.Clear();
        }

        private void AddStationaryContactNormal(in SweepBodyState state, int otherIndex, Vector2 normal)
        {
            // 낮은 우선순위 바디는 밀 수 있으므로 고정된 제약으로 취급하지 않는다.
            if (otherIndex < 0 || (sweepBodyStates[otherIndex].HasMoveDelta == false && sweepBodyStates[otherIndex].PushPriority >= state.PushPriority))
            {
                contactNormals.Add(normal);
            }
        }

        private Vector2 FindClosestNonPenetratingMoveDelta(Vector2 moveDelta)
        {
            // 표면 안쪽으로 향하지 않는 투영 후보 중 남은 이동량이 가장 큰 후보를 선택한다.
            var bestMoveDelta = Vector2.zero;
            var bestMoveDistanceSquared = 0f;

            foreach (var normal in contactNormals)
            {
                var candidate = moveDelta - normal * (Vector2.Dot(moveDelta, normal));

                if (IsMovingInto(candidate))
                {
                    continue;
                }

                var candidateDistanceSquared = candidate.sqrMagnitude;

                if (candidateDistanceSquared > bestMoveDistanceSquared)
                {
                    bestMoveDelta = candidate;
                    bestMoveDistanceSquared = candidateDistanceSquared;
                }
            }

            return bestMoveDelta;
        }

        private bool IsMovingInto(Vector2 moveDelta)
        {
            // 접촉 표면 안쪽(법선 반대 방향)으로 향하는 이동 성분이 하나라도 있는지 확인한다.
            foreach (var normal in contactNormals)
            {
                if (Vector2.Dot(moveDelta, normal) < -SweepDirectionEpsilon)
                {
                    return true;
                }
            }

            return false;
        }

        private void SlideSweepBody(int bodyIndex, Vector2 normal)
        {
            var state = sweepBodyStates[bodyIndex];
            if (state.HasMoveDelta == false)
            {
                return;
            }

            // 남은 이동량을 hit.Normal 벡터에 투영한다.
            // 남은 이동량을 법선 방향으로 투영한다 = 남는 이동량(벡터) 중에 hit.Normal 방향 성분으로 이루어진 부분의 크기를 부호가 있는 스칼라 값으로 구한다. 이 부호를 가지는 크기를 통해 투영 벡터를 구할 수 있다.(hit.Normal 방향 or -hit.Normal 방향)
            var intoSurface = Vector2.Dot(state.RemainingMoveDelta, normal);
            if (intoSurface < -SweepDirectionEpsilon)
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

        // 같은 우선순위 바디는 서로를 정지 표면으로 취급하지 않고 상대 이동으로 처리한다.
        // 양쪽에 SlideSweepBody를 적용하면 함께 움직이는 중에도 한쪽의 법선 이동만 잘려 밀던 원들이 끊길 수 있다.
        // 서로 가까워지는 법선 성분만 분담해 제거하고, 두 바디가 함께 움직이는 성분은 유지한다.
        private void SlideEqualPriorityBodies(int bodyAIndex, int bodyBIndex, Vector2 normal)
        {
            var stateA = sweepBodyStates[bodyAIndex];
            var stateB = sweepBodyStates[bodyBIndex];
            // Cast와 동일하게 최소 이동량 이하의 잔여 이동은 정지 상태로 취급한다.
            var moveDeltaA = stateA.HasMoveDelta ? stateA.RemainingMoveDelta : Vector2.zero;
            var moveDeltaB = stateB.HasMoveDelta ? stateB.RemainingMoveDelta : Vector2.zero;
            var intoSurface = Vector2.Dot(moveDeltaA - moveDeltaB, normal);
            // A/B 접촉은 B만 움직일 때 수집됐어도, 앞서 처리한 A/C 접촉이 A를 B와 같은 방향·속도로 밀 수 있다.
            // A/B를 처리할 차례에 더는 서로 파고들지 않는다면 이 접촉은 추가 슬라이드하지 않는다.
            if (intoSurface >= -SweepDirectionEpsilon)
            {
                return;
            }

            // 어느 한쪽이 슬라이드 한도를 이미 썼다면 기존 바디별 한도 정책으로 처리한다.
            if (stateA.SlideIterationCount >= MaxSlideIterationCount || stateB.SlideIterationCount >= MaxSlideIterationCount)
            {
                SlideSweepBody(bodyAIndex, normal);
                SlideSweepBody(bodyBIndex, -normal);
                return;
            }

            var weightA = Mathf.Max(0f, -Vector2.Dot(moveDeltaA, normal));
            var weightB = Mathf.Max(0f, Vector2.Dot(moveDeltaB, normal));
            var totalWeight = weightA + weightB;

            // 상대가 함께 이동하는 성분은 유지하고, 서로 가까워지는 법선 성분만 분담해 제거한다.
            // 법선 방향 이동을 조정한 바디는 슬라이드 기회를 소비한다.
            if (weightA > 0f)
            {
                stateA.RemainingMoveDelta = moveDeltaA - normal * (intoSurface * weightA / totalWeight);
                stateA.SlideIterationCount++;
                sweepBodyStates[bodyAIndex] = stateA;
            }

            // B에서 보는 상대 표면의 법선은 A가 사용하는 법선의 반대 방향이다.
            if (weightB > 0f)
            {
                stateB.RemainingMoveDelta = moveDeltaB + normal * (intoSurface * weightB / totalWeight);
                stateB.SlideIterationCount++;
                sweepBodyStates[bodyBIndex] = stateB;
            }
        }

        private void SlideLowerPriorityBody(int lowerPriorityIndex, int higherPriorityIndex, Vector2 normal)
        {
            var lowerState = sweepBodyStates[lowerPriorityIndex];
            var higherState = sweepBodyStates[higherPriorityIndex];

            // 동시 충돌에서 앞선 접촉 처리로 이동 방향이 바뀌었을 수 있으므로
            // 바뀐 이동 방향이 현재 충돌면 안쪽으로 이동하는 경우에만 남은 이동량을 0으로 만든다.
            if (lowerState.SlideIterationCount >= MaxSlideIterationCount && Vector2.Dot(higherState.RemainingMoveDelta, normal) <= 0f)
            {
                SlideSweepBody(lowerPriorityIndex, normal);
                return;
            }

            // 높은 바디의 이동은 유지하고, 낮은 바디의 상대 이동에서 표면 안쪽 성분만 제거한다.
            // 낮은 바디가 정지해 있어도 높은 바디가 접근하면 같은 법선 방향으로 밀려난다.
            var relativeMoveDelta = lowerState.RemainingMoveDelta - higherState.RemainingMoveDelta;
            var intoSurface = Vector2.Dot(relativeMoveDelta, normal);
            if (intoSurface < -SweepDirectionEpsilon)
            {
                lowerState.RemainingMoveDelta -= normal * intoSurface;
                // 자기 이동을 취소한 경우가 아니라 높은 바디의 접근 이동에 밀렸을 때만 우선순위를 전달한다.
                if (Vector2.Dot(higherState.RemainingMoveDelta, normal) > 0f)
                {
                    // 높은 바디에 밀린 이동은 슬라이드 횟수를 소비하지 않는다.
                    lowerState.PushPriority = higherState.PushPriority;
                }
                else
                {
                    // 자기 이동을 상대 표면에 맞춰 제한한 경우에만 소비한다.
                    lowerState.SlideIterationCount++;
                }

                sweepBodyStates[lowerPriorityIndex] = lowerState;
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
                if (fraction > 0f)
                {
                    // 작은 이동 델타는 현재 좌표에서 표현 가능한 간격보다 작으면 앵커에 더해도 같은 값으로 반올림될 수 있다.
                    state.AppliedMoveDelta += state.RemainingMoveDelta * fraction;
                    // 이동 델타를 시작 위치 기준으로 누적하여 다음 표현 가능한 좌표로 반올림될 만큼 쌓이면 그 위치 차이를 앵커에 적용한다.
                    dynamicBodies[bodyIndex].ApplyMovement(state.StartCenter + state.AppliedMoveDelta - dynamicBodies[bodyIndex].Center);
                }
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
