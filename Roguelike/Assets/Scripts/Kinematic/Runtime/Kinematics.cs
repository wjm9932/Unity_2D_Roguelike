using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    public static class Kinematics
    {
        public static bool CircleCast(Vector2 origin, float radius, Vector2 distance, out ShapeCastHit hit)
            => CircleCast(origin, radius, distance, null, ~0, out hit);

        public static bool CircleCast(Vector2 origin, float radius, Vector2 distance, KinematicBody ignoredBody, int layerMask, out ShapeCastHit hit)
            => KinematicSimulation.Instance.Cast.CircleCast(origin, radius, distance, ignoredBody, layerMask, out hit);

        public static bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 distance, out ShapeCastHit hit)
            => BoxCast(origin, halfExtents, distance, null, ~0, out hit);

        public static bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 distance, KinematicBody ignoredBody, int layerMask, out ShapeCastHit hit)
            => KinematicSimulation.Instance.Cast.BoxCast(origin, halfExtents, distance, ignoredBody, layerMask, out hit);

        public static bool ShapeCast(KinematicBody body, Vector2 distance, out ShapeCastHit hit)
            => ShapeCast(body, distance, ~0, out hit);

        public static bool ShapeCast(KinematicBody body, Vector2 distance, int layerMask, out ShapeCastHit hit)
            => KinematicSimulation.Instance.Cast.ShapeCast(body, distance, layerMask, out hit);

        // 결과 목록을 비운 뒤 경로상 모든 바디의 접촉을 채우고, 검출한 바디 수를 반환한다.
        public static int CircleCastAll(Vector2 origin, float radius, Vector2 distance, List<ShapeCastHit> results)
            => CircleCastAll(origin, radius, distance, null, ~0, results);

        public static int CircleCastAll(Vector2 origin, float radius, Vector2 distance, KinematicBody ignoredBody, int layerMask, List<ShapeCastHit> results)
            => KinematicSimulation.Instance.Cast.CircleCastAll(origin, radius, distance, ignoredBody, layerMask, results);

        public static int BoxCastAll(Vector2 origin, Vector2 halfExtents, Vector2 distance, List<ShapeCastHit> results)
            => BoxCastAll(origin, halfExtents, distance, null, ~0, results);

        public static int BoxCastAll(Vector2 origin, Vector2 halfExtents, Vector2 distance, KinematicBody ignoredBody, int layerMask, List<ShapeCastHit> results)
            => KinematicSimulation.Instance.Cast.BoxCastAll(origin, halfExtents, distance, ignoredBody, layerMask, results);

        public static int ShapeCastAll(KinematicBody body, Vector2 distance, List<ShapeCastHit> results)
            => ShapeCastAll(body, distance, ~0, results);

        public static int ShapeCastAll(KinematicBody body, Vector2 distance, int layerMask, List<ShapeCastHit> results)
            => KinematicSimulation.Instance.Cast.ShapeCastAll(body, distance, layerMask, results);
    }
}
