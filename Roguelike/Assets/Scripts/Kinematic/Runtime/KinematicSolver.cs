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
        // Initial과 Ghost 복구는 각각 원래 우선순위에서 시작하며, 보정 중 전달된 값은 해당 단계에서만 사용한다.
        private readonly List<int> overlapPushPriorities = new();
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
            // Teleport 요청이 있다면 Teleport부터 적용한다.
            HandleTeleport();

            // Teleport로 인한 겹침과 Solving 전부터 겹친 상태를 Solving 전에 일괄 해소한다.
            SolveOverlapCollisions();

            // 이동 요청을 적용하며 충돌을 해소한다.
            movementSolver.Solve();

            // 
            CompleteGhostRecovery();
            
            // 다이나믹 바디들의 트랜스폼을 이동 및 충돌이 해소된 위치로 동기화한다.
            SyncDynamicTransforms();
        }

        private void HandleTeleport()
        {
            foreach (var body in dynamicBodies)
            {
                body.ApplyTeleport();
            }
        }

        private void SolveOverlapCollisions()
        {
            foreach (var body in dynamicBodies)
            {
                overlapPushPriorities.Add((int)body.Priority);
            }

            for (var iteration = 0; iteration < MaxSolverIterationCount; iteration++)
            {
                var hasDynamicStaticCollision = SolveDynamicStaticOverlaps();
                var hasDynamicDynamicCollision = SolveDynamicDynamicOverlaps();

                if (hasDynamicStaticCollision == false && hasDynamicDynamicCollision == false)
                {
                    break;
                }
            }

            overlapPushPriorities.Clear();
        }

        private void CompleteGhostRecovery()
        {
            var hasRecovery = false;

            foreach (var body in dynamicBodies)
            {
                if (body.NeedsGhostRecovery == false)
                {
                    continue;
                }

                // 모든 종료 요청을 먼저 적용한 뒤 이동 후 겹침을 보정한다.
                body.ApplyGhostEnd();

                hasRecovery = true;
            }

            if (hasRecovery == false)
            {
                return;
            }

            SolveOverlapCollisions();

            // Ghost 종료 바디를 우선 보정하는 규칙은 이번 프레임에만 적용한다.
            // 다음 프레임부터는 일반 보정으로 전환해, 종료한 바디만 반복해서 밀리며 끼이는 현상을 방지한다.
            foreach (var body in dynamicBodies)
            {
                body.NeedsGhostRecovery = false;
            }
        }

        private bool SolveDynamicStaticOverlaps()
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

                    ApplyOverlapCorrection(dynamicBody, contact.SeparationMtv);
                    InheritOverlapPushPriority(bodyIndex, int.MaxValue, contact.SeparationMtv);
                    hasCollision = true;
                }
            }

            return hasCollision;
        }

        private bool SolveDynamicDynamicOverlaps()
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

                    var pushPriorityA = overlapPushPriorities[bodyAIndex];
                    var pushPriorityB = overlapPushPriorities[bodyBIndex];

                    if (pushPriorityA != pushPriorityB)
                    {
                        // 우선순위가 낮은 바디를 먼저 보정한다.
                        KinematicCollision.GetPriorityCorrections(contact, pushPriorityA, pushPriorityB, out correctionA, out correctionB);
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

                    ApplyOverlapCorrection(bodyA, correctionA);
                    ApplyOverlapCorrection(bodyB, correctionB);

                    InheritOverlapPushPriority(bodyAIndex, pushPriorityB, correctionA);
                    InheritOverlapPushPriority(bodyBIndex, overlapPushPriorities[bodyAIndex], correctionB);
                }
            }

            return hasCollision;
        }

        private void InheritOverlapPushPriority(int bodyIndex, int sourcePriority, Vector2 correction)
        {
            // 겹침 보정에서는 접근 이동량이 없어도 실제 보정이 발생하면 우선순위를 전달한다.
            if (sourcePriority > overlapPushPriorities[bodyIndex] && correction.sqrMagnitude > 0f)
            {
                overlapPushPriorities[bodyIndex] = sourcePriority;
            }
        }

        private void ApplyOverlapCorrection(KinematicBody body, Vector2 correction)
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
            overlapPushPriorities.Clear();
            movementSolver.Dispose();
        }
    }
}
