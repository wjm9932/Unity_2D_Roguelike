using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    public static class Kinematics
    {
        #region Overlap
        public static int OverlapBox(Vector2 center, Vector2 halfExtents, float angle, List<GameObject> results)
            => OverlapBox(center, halfExtents, angle, ~0, results);

        // angle은 도 단위이며 halfExtents는 박스 전체 크기의 절반이다.
        public static int OverlapBox(Vector2 center, Vector2 halfExtents, float angle, int layerMask, List<GameObject> results)
            => KinematicSimulation.Instance.Overlap.OverlapBox(center, halfExtents, angle, layerMask, results);

        public static int OverlapBox(Vector2 center, Vector2 halfExtents, Vector2 forward, List<GameObject> results)
            => OverlapBox(center, halfExtents, forward, ~0, results);

        // 로컬 Y축은 forward, 로컬 X축은 좌우다. 방향의 크기는 박스 크기에 영향을 주지 않는다.
        public static int OverlapBox(Vector2 center, Vector2 halfExtents, Vector2 forward, int layerMask, List<GameObject> results)
        {
            if (forward.x == 0f && forward.y == 0f)
            {
                results.Clear();
                return 0;
            }

            var angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f;
            return OverlapBox(center, halfExtents, angle, layerMask, results);
        }

        public static int OverlapCircle(Vector2 center, float radius, List<GameObject> results)
            => OverlapCircle(center, radius, ~0, results);

        public static int OverlapCircle(Vector2 center, float radius, int layerMask, List<GameObject> results)
            => KinematicSimulation.Instance.Overlap.OverlapCircle(center, radius, layerMask, results);
        #endregion

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
