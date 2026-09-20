using System.Collections.Generic;

namespace Kinematic.Runtime
{
    public static partial class KinematicWorld
    {
        private static readonly KinematicSolver solver = new();

        public static void Register(KinematicBody body) => solver.Register(body);

        public static void Unregister(KinematicBody body) => solver.Unregister(body);

        internal static void Solve() => solver.Solve();
    }

    public partial class KinematicWorld
    {
        private class KinematicSolver
        {
            private const int MaxSolverIterationCount = 16;

            // 추후 쿼드 트리로 전환하기 위해 미리 분리
            private readonly List<KinematicBody> staticBodies = new();
            private readonly List<KinematicBody> dynamicBodies = new();

            internal void Register(KinematicBody body)
            {
                if (body.IsStatic)
                {
                    staticBodies.Add(body);
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

            internal void Solve()
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

                SyncDynamicTransforms();
            }

            private bool SolveDynamicStaticCollisions()
            {
                var hasCollision = false;

                foreach (var dynamicBody in dynamicBodies)
                {
                    foreach(var staticBody in staticBodies)
                    {
                        if (!KinematicDetector.TryCollide(dynamicBody, staticBody, out var contact))
                        {
                            continue;
                        }

                        dynamicBody.ApplyCorrection(contact.SeparationMtv);
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

                        if (!KinematicDetector.TryCollide(bodyA, bodyB, out var contact))
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
        }
    }
}
