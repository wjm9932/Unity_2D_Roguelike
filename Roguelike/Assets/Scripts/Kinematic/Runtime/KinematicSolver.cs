using Kinematic.Interface;
using Kinematic.Runtime.Spatial;
using System.Collections.Generic;

namespace Kinematic.Runtime
{
    internal sealed class KinematicSolver
    {
        private readonly List<KinematicBody> queryResult = new();
        private readonly IQueryOnlyQuadTree<KinematicBody> staticBodies;
        private readonly IReadOnlyList<KinematicBody> dynamicBodies;
        private readonly IKinematicMovementSolver movementSolver;

        private const int MaxSolverIterationCount = 16;

        internal KinematicSolver(IQueryOnlyQuadTree<KinematicBody> statics, IReadOnlyList<KinematicBody> dynamics, bool sweep)
        {
            staticBodies = statics;
            dynamicBodies = dynamics;
            movementSolver = sweep ? new KinematicSweepSolver(statics, dynamics) : new KinematicDiscreteSolver(statics, dynamics);
        }

        internal void Solve()
        {
            // Teleport 요청이 있다면 Teleport 적용
            HandleTeleport();

            // Teleport로 인한 겹침과 Solving 전부터 겹친 상태를 Solving 전에 일괄 해소한다.
            SolveInitialCollisions();

            movementSolver.Solve();

            CompleteGhostRecovery();

            SyncDynamicTransforms();
        }

        private void HandleTeleport()
        {
            foreach (var body in dynamicBodies)
            {
                body.ApplyTeleport();
            }
        }

        private void SolveInitialCollisions()
        {
            for (var iteration = 0; iteration < MaxSolverIterationCount; iteration++)
            {
                var hasDynamicStaticCollision = SolveInitialDynamicStaticCollisions();
                var hasDynamicDynamicCollision = SolveInitialDynamicDynamicCollisions();

                if (hasDynamicStaticCollision == false && hasDynamicDynamicCollision == false)
                {
                    break;
                }
            }
        }

        private void CompleteGhostRecovery()
        {
            foreach (var body in dynamicBodies)
            {
                if (body.NeedsGhostRecovery == false)
                {
                    continue;
                }

                var hasOverlap = false;

                staticBodies.Query(body.Bounds, queryResult);

                foreach (var other in queryResult)
                {
                    if (body.CanGhostThrough(other) == false || body.CanCollideWith(other) == false)
                    {
                        continue;
                    }

                    if (body.TryCollide(other, out _))
                    {
                        hasOverlap = true;
                        break;
                    }
                }

                foreach (var other in dynamicBodies)
                {
                    if (hasOverlap)
                    {
                        break;
                    }

                    if (body == other || body.CanGhostThrough(other) == false)
                    {
                        continue;
                    }

                    if (body.CanCollideWith(other) == false)
                    {
                        continue;
                    }

                    if (body.TryCollide(other, out _))
                    {
                        hasOverlap = true;
                        break;
                    }
                }

                // 반복 한도에 걸려 겹침이 남으면 다음 프레임에도 종료한 바디만 보정한다.
                if (hasOverlap == false)
                {
                    body.NeedsGhostRecovery = false;
                }
            }
        }

        private bool SolveInitialDynamicStaticCollisions()
        {
            var hasCollision = false;

            for (var bodyIndex = 0; bodyIndex < dynamicBodies.Count; bodyIndex++)
            {
                var dynamicBody = dynamicBodies[bodyIndex];
                staticBodies.Query(dynamicBody.Bounds, queryResult);

                foreach (var staticBody in queryResult)
                {
                    if (dynamicBody.CanCollideWith(staticBody) == false || dynamicBody.TryCollide(staticBody, out var contact) == false)
                    {
                        continue;
                    }

                    dynamicBody.ApplyCorrection(contact.SeparationMtv);
                    hasCollision = true;
                }
            }

            return hasCollision;
        }

        private bool SolveInitialDynamicDynamicCollisions()
        {
            var hasCollision = false;

            for (var bodyAIndex = 0; bodyAIndex < dynamicBodies.Count; bodyAIndex++)
            {
                var bodyA = dynamicBodies[bodyAIndex];

                for (var bodyBIndex = bodyAIndex + 1; bodyBIndex < dynamicBodies.Count; bodyBIndex++)
                {
                    var bodyB = dynamicBodies[bodyBIndex];
                    if (bodyA.CanCollideWith(bodyB) == false)
                    {
                        continue;
                    }

                    // 충돌 검사
                    if (bodyA.TryCollide(bodyB, out var contact) == false)
                    {
                        continue;
                    }

                    var recoverBodyA = bodyA.NeedsGhostRecovery && bodyA.CanGhostThrough(bodyB);
                    var recoverBodyB = bodyB.NeedsGhostRecovery && bodyB.CanGhostThrough(bodyA);

                    // 한쪽만 고스트를 종료했다면 그 바디가 MTV를 전부 받아 상대를 밀지 않는다.
                    if (recoverBodyA != recoverBodyB)
                    {
                        if (recoverBodyA)
                        {
                            bodyA.ApplyCorrection(contact.SeparationMtv);
                        }
                        else
                        {
                            bodyB.ApplyCorrection(-contact.SeparationMtv);
                        }
                    }
                    else
                    {
                        // 일반 겹침은 두 바디가 MTV를 절반씩 나눠 받는다.
                        bodyA.ApplyCorrection(contact.SeparationMtv * 0.5f);
                        bodyB.ApplyCorrection(-contact.SeparationMtv * 0.5f);
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

        internal void Dispose() => movementSolver.Dispose();
    }
}
