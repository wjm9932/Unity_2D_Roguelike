using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    public static class Kinematics
    {
        #region Raycast
        public static bool Raycast(Vector2 origin, Vector2 direction, float distance, out CastHit hit)
            => Raycast(origin, direction, distance, ~0, out hit);

        public static bool Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask, out CastHit hit)
            => CircleCast(origin, 0f, direction, distance, layerMask, out hit);
        #endregion

        #region CircleCast
        public static bool CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, out CastHit hit)
            => CircleCast(origin, radius, direction, distance, ~0, out hit);

        public static bool CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, out CastHit hit)
            => KinematicSimulation.Instance.Cast.CircleCast(origin, radius, direction.normalized * distance, layerMask, out hit);

        public static int CircleCastAll(Vector2 origin, float radius, Vector2 direction, float distance, List<CastHit> results)
            => CircleCastAll(origin, radius, direction, distance, ~0, results);

        public static int CircleCastAll(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, List<CastHit> results)
            => KinematicSimulation.Instance.Cast.CircleCastAll(origin, radius, direction.normalized * distance, layerMask, results);
        #endregion

        #region BoxCast
        public static bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 direction, float distance, out CastHit hit)
            => BoxCast(origin, halfExtents, direction, distance, ~0, out hit);

        public static bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 direction, float distance, int layerMask, out CastHit hit)
            => KinematicSimulation.Instance.Cast.BoxCast(origin, halfExtents, direction.normalized * distance, layerMask, out hit);

        public static int BoxCastAll(Vector2 origin, Vector2 halfExtents, Vector2 direction, float distance, List<CastHit> results)
            => BoxCastAll(origin, halfExtents, direction, distance, ~0, results);

        public static int BoxCastAll(Vector2 origin, Vector2 halfExtents, Vector2 direction, float distance, int layerMask, List<CastHit> results)
            => KinematicSimulation.Instance.Cast.BoxCastAll(origin, halfExtents, direction.normalized * distance, layerMask, results);
        #endregion

        #region ShapeCast
        public static bool ShapeCast(this KinematicBody body, Vector2 direction, float distance, out CastHit hit)
            => ShapeCast(body, direction, distance, ~0, out hit);

        public static bool ShapeCast(this KinematicBody body, Vector2 direction, float distance, int layerMask, out CastHit hit)
            => KinematicSimulation.Instance.Cast.ShapeCast(body, direction.normalized * distance, layerMask, out hit);

        public static int ShapeCastAll(this KinematicBody body, Vector2 direction, float distance, List<CastHit> results)
            => ShapeCastAll(body, direction, distance, ~0, results);

        public static int ShapeCastAll(this KinematicBody body, Vector2 direction, float distance, int layerMask, List<CastHit> results)
            => KinematicSimulation.Instance.Cast.ShapeCastAll(body, direction.normalized * distance, layerMask, results);
        #endregion
    }
}
