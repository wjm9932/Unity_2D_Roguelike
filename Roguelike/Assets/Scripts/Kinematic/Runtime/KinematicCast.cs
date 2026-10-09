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
        private static readonly IComparer<ShapeCastHit> hitDistanceComparer = Comparer<ShapeCastHit>.Create((hitA, hitB) => hitA.Distance.CompareTo(hitB.Distance));

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
            return ShapeCast(origin, shape, distance, ignoredBody, layerMask, out hit);
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
            return ShapeCast(origin, shape, distance, ignoredBody, layerMask, out hit);
        }

        internal bool ShapeCast(KinematicBody body, Vector2 distance, int layerMask, out ShapeCastHit hit) => ShapeCast(body.Center, body.Shape, distance, body, layerMask, out hit);

        internal int CircleCastAll(Vector2 origin, float radius, Vector2 distance, KinematicBody ignoredBody, int layerMask, List<ShapeCastHit> results)
        {
            Debug.Assert(radius >= 0f, $"CircleCastAll radius must be non-negative: {radius}");

            if (radius < 0f)
            {
                results.Clear();

                return 0;
            }

            var shape = ColliderInfo.CreateCircle(Vector2.zero, radius);

            return ShapeCastAll(origin, shape, distance, ignoredBody, layerMask, results);
        }

        internal int BoxCastAll(Vector2 origin, Vector2 halfExtents, Vector2 distance, KinematicBody ignoredBody, int layerMask, List<ShapeCastHit> results)
        {
            Debug.Assert(halfExtents.x >= 0f && halfExtents.y >= 0f, $"BoxCastAll half extents must be non-negative: {halfExtents}");

            if (halfExtents.x < 0f || halfExtents.y < 0f)
            {
                results.Clear();

                return 0;
            }

            var shape = ColliderInfo.CreateBox(Vector2.zero, halfExtents);

            return ShapeCastAll(origin, shape, distance, ignoredBody, layerMask, results);
        }

        internal int ShapeCastAll(KinematicBody body, Vector2 distance, int layerMask, List<ShapeCastHit> results)
            => ShapeCastAll(body.Center, body.Shape, distance, body, layerMask, results);

        private bool ShapeCast(Vector2 origin, in ColliderInfo shape, Vector2 distance, KinematicBody ignoredBody, int layerMask, out ShapeCastHit hit)
        {
            hit = default;

            if (QueryCandidates(origin, shape, distance) == false)
            {
                return false;
            }

            var hasHit = false;
            var closestFraction = float.PositiveInfinity;

            foreach (var candidate in queryResult)
            {
                if (TryCastCandidate(origin, shape, distance, ignoredBody, candidate, layerMask, out var candidateHit) == false || candidateHit.Fraction >= closestFraction)
                {
                    continue;
                }

                closestFraction = candidateHit.Fraction;
                hit = candidateHit;
                hasHit = true;
            }

            return hasHit;
        }

        private int ShapeCastAll(Vector2 origin, in ColliderInfo shape, Vector2 distance, KinematicBody ignoredBody, int layerMask, List<ShapeCastHit> results)
        {
            // 이전 조회 결과를 비우고 이번 경로의 바디별 첫 접촉을 수집한다.
            results.Clear();

            if (QueryCandidates(origin, shape, distance) == false)
            {
                return 0;
            }

            foreach (var candidate in queryResult)
            {
                if (TryCastCandidate(origin, shape, distance, ignoredBody, candidate, layerMask, out var candidateHit))
                {
                    results.Add(candidateHit);
                }
            }

            results.Sort(hitDistanceComparer);

            return results.Count;
        }

        private bool QueryCandidates(Vector2 origin, in ColliderInfo shape, Vector2 distance)
        {
            queryResult.Clear();

            if (staticBodies == null || distance.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            var extents = shape.Shape switch
            {
                Shape.Circle => Vector2.one * shape.Radius,
                Shape.Box => shape.HalfExtents,
                _ => Vector2.zero
            };
            // 쿼드 트리 조회용 이동경로 AABB
            var sweptBounds = AABB.Create(origin, extents).GetSweptBounds(distance);

            staticBodies.Query(sweptBounds, queryResult);

            foreach (var candidate in dynamicBodies)
            {
                if (candidate.Bounds.Overlaps(sweptBounds))
                {
                    queryResult.Add(candidate);
                }
            }

            return queryResult.Count > 0;
        }

        private static bool TryCastCandidate(Vector2 origin, in ColliderInfo shape, Vector2 moveDelta, KinematicBody ignoredBody, KinematicBody candidate, int layerMask, out ShapeCastHit hit)
        {
            hit = default;

            if (candidate == ignoredBody || (layerMask & (1 << candidate.Layer)) == 0)
            {
                return false;
            }

            return KinematicShapeCast.TryCast(origin, shape, candidate, moveDelta, out hit);
        }
    }
}
