using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    public static class Kinematics
    {
        public static bool Raycast(Vector2 origin, Vector2 moveDelta, out CastHit hit)
            => Raycast(origin, moveDelta, null, ~0, out hit);

        public static bool Raycast(Vector2 origin, Vector2 moveDelta, KinematicBody ignoredBody, int layerMask, out CastHit hit)
            => CircleCast(origin, 0f, moveDelta, ignoredBody, layerMask, out hit);

        public static bool CircleCast(Vector2 origin, float radius, Vector2 distance, out CastHit hit)
            => CircleCast(origin, radius, distance, null, ~0, out hit);

        public static bool CircleCast(Vector2 origin, float radius, Vector2 distance, KinematicBody ignoredBody, int layerMask, out CastHit hit)
            => KinematicSimulation.Instance.Cast.CircleCast(origin, radius, distance, ignoredBody, layerMask, out hit);

        public static bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 distance, out CastHit hit)
            => BoxCast(origin, halfExtents, distance, null, ~0, out hit);

        public static bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 distance, KinematicBody ignoredBody, int layerMask, out CastHit hit)
            => KinematicSimulation.Instance.Cast.BoxCast(origin, halfExtents, distance, ignoredBody, layerMask, out hit);

        public static bool ShapeCast(KinematicBody body, Vector2 distance, out CastHit hit)
            => ShapeCast(body, distance, ~0, out hit);

        public static bool ShapeCast(KinematicBody body, Vector2 distance, int layerMask, out CastHit hit)
            => KinematicSimulation.Instance.Cast.ShapeCast(body, distance, layerMask, out hit);

        public static int CircleCastAll(Vector2 origin, float radius, Vector2 distance, List<CastHit> results)
            => CircleCastAll(origin, radius, distance, null, ~0, results);

        public static int CircleCastAll(Vector2 origin, float radius, Vector2 distance, KinematicBody ignoredBody, int layerMask, List<CastHit> results)
            => KinematicSimulation.Instance.Cast.CircleCastAll(origin, radius, distance, ignoredBody, layerMask, results);

        public static int BoxCastAll(Vector2 origin, Vector2 halfExtents, Vector2 distance, List<CastHit> results)
            => BoxCastAll(origin, halfExtents, distance, null, ~0, results);

        public static int BoxCastAll(Vector2 origin, Vector2 halfExtents, Vector2 distance, KinematicBody ignoredBody, int layerMask, List<CastHit> results)
            => KinematicSimulation.Instance.Cast.BoxCastAll(origin, halfExtents, distance, ignoredBody, layerMask, results);

        public static int ShapeCastAll(KinematicBody body, Vector2 distance, List<CastHit> results)
            => ShapeCastAll(body, distance, ~0, results);

        public static int ShapeCastAll(KinematicBody body, Vector2 distance, int layerMask, List<CastHit> results)
            => KinematicSimulation.Instance.Cast.ShapeCastAll(body, distance, layerMask, results);
    }
}
