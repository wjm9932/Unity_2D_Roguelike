using Kinematic.Data;
using Kinematic.Interface;
using Kinematic.Runtime.Spatial;
using System.Collections.Generic;
using System.Linq;
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
        // 모든 dynamicBodies와 같은 순서로 이동량을 저장하여 동일한 인덱스로 연결한다.
        private readonly List<Vector2> discreteMoveDeltas = new();

        private const int MaxSolverIterationCount = 16;
        private const float MinMoveDistance = 0.000001f;

        internal KinematicDiscreteSolver(IQueryOnlyQuadTree<KinematicBody> statics, IReadOnlyList<KinematicBody> dynamics)
        {
            staticBodies = statics;
            dynamicBodies = dynamics;
        }

        public void Solve()
        {
            SolveDiscreteMovements();
            ResolveDiscreteCollisions();
            discreteMoveDeltas.Clear();
        }

        private void SolveDiscreteMovements()
        {
            foreach (var body in dynamicBodies)
            {
                var moveDelta = body.ConsumeMoveDelta();
                discreteMoveDeltas.Add(moveDelta);
                body.ApplyMovement(moveDelta);
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
                    discreteMoveDeltas[bodyIndex] += contact.SeparationMtv;
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

                    // 충돌 검사
                    if (!bodyA.TryCollide(bodyB, out var contact))
                    {
                        continue;
                    }

                    // 서로 지나쳐 접촉 법선이 뒤집히면 접근한 바디 대신 상대 바디가 보정될 수 있다.
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

                    hasCollision = true;
                }
            }

            return hasCollision;
        }
        public void Dispose()
        {
        }
    }
}
