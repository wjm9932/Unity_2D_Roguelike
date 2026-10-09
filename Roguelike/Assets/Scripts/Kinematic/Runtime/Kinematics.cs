using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    public static class Kinematics
    {
        public static bool CircleCast(Vector2 origin, float radius, Vector2 distance, out ShapeCastHit hit, KinematicBody ignoredBody = null, int layerMask = ~0)
            => KinematicSimulation.Instance.Cast.CircleCast(origin, radius, distance, ignoredBody, layerMask, out hit);

        public static bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 distance, out ShapeCastHit hit, KinematicBody ignoredBody = null, int layerMask = ~0)
            => KinematicSimulation.Instance.Cast.BoxCast(origin, halfExtents, distance, ignoredBody, layerMask, out hit);

        public static bool ShapeCast(KinematicBody body, Vector2 distance, out ShapeCastHit hit, int layerMask = ~0)
            => KinematicSimulation.Instance.Cast.ShapeCast(body, distance, layerMask, out hit);

        // 결과 목록을 비운 뒤 충돌 거리 오름차순으로 채우고, 검출한 바디 수를 반환한다.
        public static int CircleCastAll(Vector2 origin, float radius, Vector2 distance, List<ShapeCastHit> results, KinematicBody ignoredBody = null, int layerMask = ~0)
            => KinematicSimulation.Instance.Cast.CircleCastAll(origin, radius, distance, ignoredBody, layerMask, results);

        public static int BoxCastAll(Vector2 origin, Vector2 halfExtents, Vector2 distance, List<ShapeCastHit> results, KinematicBody ignoredBody = null, int layerMask = ~0)
            => KinematicSimulation.Instance.Cast.BoxCastAll(origin, halfExtents, distance, ignoredBody, layerMask, results);

        public static int ShapeCastAll(KinematicBody body, Vector2 distance, List<ShapeCastHit> results, int layerMask = ~0)
            => KinematicSimulation.Instance.Cast.ShapeCastAll(body, distance, layerMask, results);
    }
}
