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
    }
}
