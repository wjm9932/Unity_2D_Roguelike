using Kinematic.Data;
using Kinematic.Runtime.Spatial;
using System.Collections.Generic;

namespace Kinematic.Runtime
{
    public static partial class KinematicWorld
    {
        private static readonly KinematicSolver solver = new();

        public static void Init(AABB bounds) => solver.Init(bounds);

        public static void Register(KinematicBody body) => solver.Register(body);

        public static void Unregister(KinematicBody body) => solver.Unregister(body);

        internal static void Solve() => solver.Solve();
    }

    public partial class KinematicWorld
    {
        private class KinematicSolver
        {
            private const int MaxSolverIterationCount = 16;

            private  QuadTree staticBodies;
            private readonly List<KinematicBody> dynamicBodies = new();
            private readonly List<KinematicBody> queryResult = new();

            internal void Init(AABB bounds)
            {
                staticBodies = new QuadTree(bounds);
            }

            internal void Register(KinematicBody body)
            {
                if (body.IsStatic)
                {
                    staticBodies.Insert(body);
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

                    queryResult.Clear();
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

                        if (!KinematicBodyExtension.TryCollide(bodyA, bodyB, out var contact))
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
