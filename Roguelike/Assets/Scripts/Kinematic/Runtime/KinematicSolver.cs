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

                if (!hasDynamicStaticCollision && !hasDynamicDynamicCollision)
                {
                    break;
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

        private bool SolveInitialDynamicDynamicCollisions()
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

        internal void Dispose() => movementSolver.Dispose();
    }
}
