using UnityEngine;

namespace Kinematic.Runtime
{
    public static class Kinematics
    {
        private static readonly KinematicSolver solver = KinematicSolver.Instance;

        public static bool CircleCast(Vector2 origin, float radius, Vector2 distance, out ShapeCastHit hit, KinematicBody ignoredBody = null) => solver.CircleCast(origin, radius, distance, ignoredBody, out hit);

        public static bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 distance, out ShapeCastHit hit, KinematicBody ignoredBody = null) => solver.BoxCast(origin, halfExtents, distance, ignoredBody, out hit);

        public static bool ShapeCast(KinematicBody body, Vector2 distance, out ShapeCastHit hit) => solver.ShapeCast(body, distance, out hit);
    }
}
