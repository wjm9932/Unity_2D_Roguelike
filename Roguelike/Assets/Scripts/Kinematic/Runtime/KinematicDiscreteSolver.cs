using Kinematic.Interface;
using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal sealed class KinematicDiscreteSolver : IKinematicMovementSolver
    {
        // 정적 바디 조회용 쿼드 트리
        private readonly IQueryOnlyQuadTree<KinematicBody> staticBodies;
        // 동적 바디 조회용 리스트
        private readonly IReadOnlyList<KinematicBody> dynamicBodies;
        // 쿼리 결과용 버퍼
        private readonly List<KinematicBody> queryResult = new();
        // 모든 dynamicBodies와 같은 순서로 이동량과 우선순위를 저장한다.
        private readonly List<DiscreteBodyState> discreteBodyStates = new();

        private int MaxSolverIterationCount => KinematicWorld.Settings.MaxSolverIterationCount;
        private const float MinMoveDistance = 0.000001f;

        private struct DiscreteBodyState
        {
            internal Vector2 MoveDelta { get; set; }
            internal int PushPriority { get; private set; }
            // 보정으로 밀린 바디도 이후 검사에 참여하고, 보정으로 이동이 상쇄되면 제외한다.
            internal bool HasMoveDelta => MoveDelta.sqrMagnitude > MinMoveDistance * MinMoveDistance;

            internal DiscreteBodyState(Vector2 moveDelta, int pushPriority)
            {
                MoveDelta = moveDelta;
                PushPriority = pushPriority;
            }

            internal void InheritPushPriority(int sourcePriority, Vector2 sourceMoveDelta, Vector2 correction)
            {
                // 상대의 접근 이동이 만든 밀림만 전달한다. 자기 이동을 취소한 보정은 제외한다.
                if (sourcePriority > PushPriority && Vector2.Dot(sourceMoveDelta, correction) > 0f && correction.sqrMagnitude > 0f)
                {
                    PushPriority = sourcePriority;
                }
            }
        }

        internal KinematicDiscreteSolver(IQueryOnlyQuadTree<KinematicBody> statics, IReadOnlyList<KinematicBody> dynamics)
        {
            staticBodies = statics;
            dynamicBodies = dynamics;
        }

        public void Solve()
        {
            SolveDiscreteMovements();
            ResolveDiscreteCollisions();
        }

        private void SolveDiscreteMovements()
        {
            foreach (var body in dynamicBodies)
            {
                var moveDelta = body.ConsumeMoveDelta();
                discreteBodyStates.Add(new DiscreteBodyState(moveDelta, (int)body.Priority));
            }

            for (var bodyIndex = 0; bodyIndex < dynamicBodies.Count; bodyIndex++)
            {
                dynamicBodies[bodyIndex].ApplyMovement(discreteBodyStates[bodyIndex].MoveDelta);
            }
        }

        private void ResolveDiscreteCollisions()
        {
            for (var iteration = 0; iteration < MaxSolverIterationCount; iteration++)
            {
                var hasDynamicStaticCollision = SolveDynamicStaticCollisions();
                var hasDynamicDynamicCollision = SolveDynamicDynamicCollisions();

                if (hasDynamicStaticCollision == false && hasDynamicDynamicCollision == false)
                {
                    break;
                }
            }

            discreteBodyStates.Clear();
        }

        // 이동량이 있는 동적 바디만 정적 바디와 검사한다.
        private bool SolveDynamicStaticCollisions()
        {
            var hasCollision = false;

            for (var bodyIndex = 0; bodyIndex < dynamicBodies.Count; bodyIndex++)
            {
                if (discreteBodyStates[bodyIndex].HasMoveDelta == false)
                {
                    continue;
                }

                var dynamicBody = dynamicBodies[bodyIndex];

                staticBodies.Query(dynamicBody.Bounds, queryResult);

                foreach (var staticBody in queryResult)
                {
                    if (discreteBodyStates[bodyIndex].HasMoveDelta == false)
                    {
                        break;
                    }

                    if (dynamicBody.CanCollideWith(staticBody) == false || dynamicBody.IsTriggerPair(staticBody) || dynamicBody.TryCollide(staticBody, out var contact) == false)
                    {
                        continue;
                    }

                    dynamicBody.ApplyCorrection(contact.SeparationMtv);
                    var state = discreteBodyStates[bodyIndex];
                    state.MoveDelta += contact.SeparationMtv;
                    discreteBodyStates[bodyIndex] = state;
                    hasCollision = true;
                }
            }

            return hasCollision;
        }

        // 두 바디 모두 이동량이 없으면 동적 쌍의 검사를 건너뛴다.
        private bool SolveDynamicDynamicCollisions()
        {
            var hasCollision = false;

            for (var bodyAIndex = 0; bodyAIndex < dynamicBodies.Count; bodyAIndex++)
            {
                var bodyA = dynamicBodies[bodyAIndex];

                for (var bodyBIndex = bodyAIndex + 1; bodyBIndex < dynamicBodies.Count; bodyBIndex++)
                {
                    if (discreteBodyStates[bodyAIndex].HasMoveDelta == false && discreteBodyStates[bodyBIndex].HasMoveDelta == false)
                    {
                        continue;
                    }

                    var bodyB = dynamicBodies[bodyBIndex];

                    if (bodyA.CanCollideWith(bodyB) == false || bodyA.IsTriggerPair(bodyB))
                    {
                        continue;
                    }

                    // 충돌 검사
                    if (bodyA.TryCollide(bodyB, out var contact) == false)
                    {
                        continue;
                    }

                    hasCollision = true;

                    var stateA = discreteBodyStates[bodyAIndex];
                    var stateB = discreteBodyStates[bodyBIndex];
                    if (stateA.PushPriority != stateB.PushPriority)
                    {
                        // 낮은 우선순위 바디를 먼저 보정하고, 새로 발생한 겹침은 이후 Iteration에서 해결한다.
                        KinematicCollision.GetPriorityCorrections(contact, stateA.PushPriority, stateB.PushPriority, out var priorityCorrectionA, out var priorityCorrectionB);

                        bodyA.ApplyCorrection(priorityCorrectionA);
                        bodyB.ApplyCorrection(priorityCorrectionB);

                        stateA.MoveDelta += priorityCorrectionA;
                        stateB.MoveDelta += priorityCorrectionB;

                        stateA.InheritPushPriority(stateB.PushPriority, stateB.MoveDelta, priorityCorrectionA);
                        stateB.InheritPushPriority(stateA.PushPriority, stateA.MoveDelta, priorityCorrectionB);
                    }
                    else
                    {
                        // 서로 지나쳐 접촉 법선이 뒤집히면 접근한 바디 대신 상대 바디가 보정될 수 있다.
                        var moveDeltaA = stateA.MoveDelta;
                        var moveDeltaB = stateB.MoveDelta;
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

                        stateA.MoveDelta += correctionA;
                        stateB.MoveDelta += correctionB;
                    }

                    discreteBodyStates[bodyAIndex] = stateA;
                    discreteBodyStates[bodyBIndex] = stateB;
                }
            }

            return hasCollision;
        }

        public void Dispose()
        {
            discreteBodyStates.Clear();
        }
    }
}
