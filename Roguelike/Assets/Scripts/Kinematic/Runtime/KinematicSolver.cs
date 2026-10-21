using Kinematic.Data;
using Kinematic.Interface;
using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal sealed class KinematicSolver
    {
        private readonly List<KinematicBody> queryResult = new();
        private readonly IQueryOnlyQuadTree<KinematicBody> staticBodies;
        private readonly IReadOnlyList<KinematicBody> dynamicBodies;
        private readonly IKinematicMovementSolver movementSolver;
        // Initial에서 전달받은 우선순위는 Initial 내부에서만 사용한다.
        private readonly List<int> initialPushPriorities = new();
        private CollisionDetection CollisionDetection => KinematicWorld.Settings.CollisionDetection;
        private OverlapResolutionMode OverlapResolutionMode => KinematicWorld.Settings.OverlapResolutionMode;

        private const int MaxSolverIterationCount = 2;

        internal KinematicSolver(IQueryOnlyQuadTree<KinematicBody> statics, IReadOnlyList<KinematicBody> dynamics)
        {
            staticBodies = statics;
            dynamicBodies = dynamics;
            movementSolver = CollisionDetection == CollisionDetection.Sweep ? new KinematicSweepSolver(statics, dynamics) : new KinematicDiscreteSolver(statics, dynamics);
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
            foreach (var body in dynamicBodies)
            {
                initialPushPriorities.Add((int)body.Priority);
            }

            for (var iteration = 0; iteration < MaxSolverIterationCount; iteration++)
            {
                var hasDynamicStaticCollision = SolveInitialDynamicStaticCollisions();
                var hasDynamicDynamicCollision = SolveInitialDynamicDynamicCollisions();

                if (hasDynamicStaticCollision == false && hasDynamicDynamicCollision == false)
                {
                    break;
                }
            }

            initialPushPriorities.Clear();
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

                    ApplyInitialCorrection(dynamicBody, contact.SeparationMtv);
                    InheritInitialPushPriority(bodyIndex, int.MaxValue, contact.SeparationMtv);
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

                    hasCollision = true;

                    var recoverBodyA = bodyA.NeedsGhostRecovery && bodyA.CanGhostThrough(bodyB);
                    var recoverBodyB = bodyB.NeedsGhostRecovery && bodyB.CanGhostThrough(bodyA);

                    var correctionA = Vector2.zero;
                    var correctionB = Vector2.zero;

                    var pushPriorityA = initialPushPriorities[bodyAIndex];
                    var pushPriorityB = initialPushPriorities[bodyBIndex];

                    if (pushPriorityA != pushPriorityB)
                    {
                        // 우선순위가 낮은 바디를 먼저 보정한다.
                        KinematicBodyExtension.GetPriorityCorrections(contact, pushPriorityA, pushPriorityB, out correctionA, out correctionB);
                    }
                    // 같은 우선순위에서 한쪽만 고스트를 종료했다면 기존처럼 종료한 바디만 보정한다.
                    else if (recoverBodyA != recoverBodyB)
                    {
                        correctionA = recoverBodyA ? contact.SeparationMtv : Vector2.zero;
                        correctionB = recoverBodyB ? -contact.SeparationMtv : Vector2.zero;
                    }
                    else
                    {
                        // 일반 겹침은 두 바디가 MTV를 절반씩 나눠 받는다.
                        correctionA = contact.SeparationMtv * 0.5f;
                        correctionB = -contact.SeparationMtv * 0.5f;
                    }

                    ApplyInitialCorrection(bodyA, correctionA);
                    ApplyInitialCorrection(bodyB, correctionB);

                    InheritInitialPushPriority(bodyAIndex, pushPriorityB, correctionA);
                    InheritInitialPushPriority(bodyBIndex, initialPushPriorities[bodyAIndex], correctionB);
                }
            }

            return hasCollision;
        }

        private void InheritInitialPushPriority(int bodyIndex, int sourcePriority, Vector2 correction)
        {
            // Initial에서는 접근 이동량이 없어도 실제 보정이 발생하면 우선순위를 전달한다.
            if (sourcePriority > initialPushPriorities[bodyIndex] && correction.sqrMagnitude > 0f)
            {
                initialPushPriorities[bodyIndex] = sourcePriority;
            }
        }

        private void ApplyInitialCorrection(KinematicBody body, Vector2 correction)
        {
            if (OverlapResolutionMode == OverlapResolutionMode.Smooth)
            {
                body.ApplySmoothCorrection(correction);
            }
            else
            {
                body.ApplyCorrection(correction);
            }
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
            initialPushPriorities.Clear();
            movementSolver.Dispose();
        }
    }
}
