using Kinematic.Data;
using Kinematic.Interface;
using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal sealed class KinematicOverlap
    {
        private readonly IQueryOnlyQuadTree<KinematicBody> staticBodies;
        private readonly IReadOnlyList<KinematicBody> dynamicBodies;
        private readonly List<KinematicBody> queryResult = new();

        internal KinematicOverlap(IQueryOnlyQuadTree<KinematicBody> statics, IReadOnlyList<KinematicBody> dynamics)
        {
            staticBodies = statics;
            dynamicBodies = dynamics;
        }

        internal int OverlapBox(Vector2 center, Vector2 halfExtents, float angle, int layerMask, List<GameObject> results)
        {
            results.Clear();
            
            if (halfExtents.x < 0f || halfExtents.y < 0f)
            {
                return 0;
            }

            var radians = angle * Mathf.Deg2Rad;
            var cos = Mathf.Abs(Mathf.Cos(radians));
            var sin = Mathf.Abs(Mathf.Sin(radians));
            var extents = new Vector2(cos * halfExtents.x + sin * halfExtents.y, sin * halfExtents.x + cos * halfExtents.y);

            return Overlap(center, ColliderInfo.CreateBox(Vector2.zero, halfExtents), angle, AABB.Create(center, extents), layerMask, results);
        }

        internal int OverlapCircle(Vector2 center, float radius, int layerMask, List<GameObject> results)
        {
            results.Clear();
         
            if (radius < 0f)
            {
                return 0;
            }

            return Overlap(center, ColliderInfo.CreateCircle(Vector2.zero, radius), 0f, AABB.Create(center, Vector2.one * radius), layerMask, results);
        }

        private int Overlap(Vector2 center, in ColliderInfo shape, float angle, in AABB bounds, int layerMask, List<GameObject> results)
        {
            staticBodies.Query(bounds, queryResult);

            foreach (var candidate in dynamicBodies)
            {
                if (candidate.Bounds.Overlaps(bounds))
                {
                    queryResult.Add(candidate);
                }
            }

            foreach (var candidate in queryResult)
            {
                if ((layerMask & (1 << candidate.Layer)) == 0)
                {
                    continue;
                }

                var overlaps = shape.Shape switch
                {
                    Shape.Box when candidate.Shape.Shape == Shape.Box =>
                        KinematicCollision.TryBoxBox(center, shape.HalfExtents, angle, candidate.Center, candidate.Shape.HalfExtents, out _),
                    Shape.Box when candidate.Shape.Shape == Shape.Circle =>
                        KinematicCollision.TryBoxCircle(center, shape.HalfExtents, angle, candidate.Center, candidate.Shape.Radius, out _),
                    // OverlapCircle을 통해 호출된 경우에는 OBB 검사가 아닌 AABB로 검사한다. 오브젝트들은 전부 AABB로 세팅되어있기때문
                    Shape.Circle => KinematicCollision.TryGetContact(center, shape, candidate.Center, candidate.Shape, out _),
                    _ => false
                };

                if (overlaps)
                {
                    results.Add(candidate.Transform.gameObject);
                }
            }

            return results.Count;
        }
    }
}
