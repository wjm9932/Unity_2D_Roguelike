using Kinematic.Data;
using Kinematic.Interface;
using Kinematic.Runtime.Spatial;
using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal sealed class KinematicCast
    {
        // 정적 바디 조회용 쿼드 트리
        private readonly IQueryOnlyQuadTree<KinematicBody> staticBodies;
        // 동적 바디 조회용 읽기 전용 리스트
        private readonly IReadOnlyList<KinematicBody> dynamicBodies;
        // 쿼리 결과용 버퍼
        private readonly List<KinematicBody> queryResult = new();

        internal KinematicCast(IQueryOnlyQuadTree<KinematicBody> statics, IReadOnlyList<KinematicBody> dynamics)
        {
            staticBodies = statics;
            dynamicBodies = dynamics;
        }

        internal bool CircleCast(Vector2 origin, float radius, Vector2 distance, KinematicBody ignoredBody, int layerMask, out ShapeCastHit hit)
        {
            Debug.Assert(radius >= 0f, $"CircleCast radius must be non-negative: {radius}");

            if (radius < 0f)
            {
                hit = default;
                return false;
            }

            var shape = ColliderInfo.CreateCircle(Vector2.zero, radius);
            return ShapeCast(origin, shape, distance, ignoredBody, queryDynamicBodies: true, layerMask, out hit);
        }

        internal bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 distance, KinematicBody ignoredBody, int layerMask, out ShapeCastHit hit)
        {
            Debug.Assert(halfExtents.x >= 0f && halfExtents.y >= 0f, $"BoxCast half extents must be non-negative: {halfExtents}");

            if (halfExtents.x < 0f || halfExtents.y < 0f)
            {
                hit = default;
                return false;
            }

            var shape = ColliderInfo.CreateBox(Vector2.zero, halfExtents);
            return ShapeCast(origin, shape, distance, ignoredBody, queryDynamicBodies: true, layerMask, out hit);
        }

        internal bool ShapeCast(KinematicBody body, Vector2 distance, int layerMask, out ShapeCastHit hit) => ShapeCast(body.Center, body.Shape, distance, body, queryDynamicBodies: true, layerMask, out hit);

        private bool ShapeCast(Vector2 origin, in ColliderInfo shape, Vector2 distance, KinematicBody ignoredBody, bool queryDynamicBodies, int layerMask, out ShapeCastHit hit)
        {
            hit = default;

            if (staticBodies == null || distance.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            var hasHit = false;
            var closestFraction = float.PositiveInfinity;
            var extents = shape.Shape switch
            {
                Shape.Circle => Vector2.one * shape.Radius,
                Shape.Box => shape.HalfExtents,
                _ => Vector2.zero
            };
            // 쿼드 트리 조회용 이동경로 AABB
            var sweptBounds = AABB.Create(origin, extents).GetSweptBounds(distance);

            queryResult.Clear();
            staticBodies.Query(sweptBounds, queryResult);

            foreach (var candidate in queryResult)
            {
                TryUpdateClosestHit(origin, shape, distance, ignoredBody, candidate, layerMask, ref closestFraction, ref hit, ref hasHit);
            }

            queryResult.Clear();

            if (queryDynamicBodies == false)
            {
                return hasHit;
            }

            foreach (var candidate in dynamicBodies)
            {
                if (!candidate.Bounds.Overlaps(sweptBounds))
                {
                    continue;
                }

                TryUpdateClosestHit(origin, shape, distance, ignoredBody, candidate, layerMask, ref closestFraction, ref hit, ref hasHit);
            }

            return hasHit;
        }

        private static void TryUpdateClosestHit(Vector2 origin, in ColliderInfo shape, Vector2 moveDelta, KinematicBody ignoredBody, KinematicBody candidate, int layerMask, ref float closestFraction, ref ShapeCastHit closestHit, ref bool hasHit)
        {
            if (candidate == ignoredBody || (layerMask & (1 << candidate.Layer)) == 0)
            {
                return;
            }

            if (KinematicShapeCast.TryCast(origin, shape, candidate, moveDelta, out var candidateHit) == false || candidateHit.Fraction >= closestFraction)
            {
                return;
            }

            closestFraction = candidateHit.Fraction;
            closestHit = candidateHit;
            hasHit = true;
        }
    }
}
