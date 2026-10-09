using Kinematic.Interface;
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
            // Teleport 요청 적용
            HandleTeleport();

            // Teleport로 분리된 기존 겹침 기록 정리
            TryRemoveSeparatedGhostOverlaps();

            // Ghost 종료 시 겹친 쌍을 기록하여 초기 MTV에서 제외
            UpdateGhostOverlaps();

            // 초기 겹침 해소
            SolveInitialCollisions();

            // 초기 보정으로 분리된 겹침 기록 정리
            TryRemoveSeparatedGhostOverlaps();

            // 이동 처리
            movementSolver.Solve();

            // 이동으로 분리된 겹침 기록 정리
            TryRemoveSeparatedGhostOverlaps();

            // Transform 동기화
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

        private void UpdateGhostOverlaps()
        {
            foreach (var body in dynamicBodies)
            {
                // 고스트가 끝나서 겹침을 체크해야하는지 여부
                if (body.Ghost.NeedsGhostOverlapCheck == false) continue;

                staticBodies.Query(body.Bounds, queryResult);

                foreach (var other in queryResult)
                {
                    TryTrackGhostOverlap(body, other);
                }

                foreach (var other in dynamicBodies)
                {
                    TryTrackGhostOverlap(body, other);
                }

                body.Ghost.CompleteGhostOverlapCheck();
            }
        }

        private static void TryTrackGhostOverlap(KinematicBody body, KinematicBody other)
        {
            if (body == other || body.Ghost.CanGhostThrough(other) == false)
            {
                return;
            }

            if (body.CanCollideWith(other) == false)
            {
                return;
            }

            // 대쉬 종료 시 이미 겹친 쌍에만 MTV 예외를 적용한다.
            if (body.TryCollide(other, out _))
            {
                // 고스트가 끝난 후 겹쳐있는 pawn들을 수집한다.
                body.Ghost.TrackGhostOverlap(other);
            }
        }

        private void TryRemoveSeparatedGhostOverlaps()
        {
            foreach (var body in dynamicBodies)
            {
                body.Ghost.TryRemoveSeparatedGhostOverlaps();
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
                    if (dynamicBody.Ghost.HasGhostOverlap(staticBody)) continue;

                    if (!dynamicBody.CanCollideWith(staticBody) || !dynamicBody.TryCollide(staticBody, out var contact))
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

                    if (bodyA.Ghost.HasGhostOverlap(bodyB))
                    {
                        continue;
                    }

                    if (bodyA.CanCollideWith(bodyB) == false)
                    {
                        continue;
                    }

                    // 충돌 검사
                    if (bodyA.TryCollide(bodyB, out var contact) == false)
                    {
                        continue;
                    }

                    // 두 동적 바디가 초기 겹침의 MTV를 절반씩 분담한다.
                    bodyA.ApplyCorrection(contact.SeparationMtv * 0.5f);
                    bodyB.ApplyCorrection(-contact.SeparationMtv * 0.5f);

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
