using UnityEngine;

namespace Kinematic.Runtime
{
    public static class Kinematics
    {
        private static readonly KinematicSolver solver = KinematicSolver.Instance;

        public static bool CircleCast(Vector2 origin, float radius, Vector2 moveDelta, out ShapeCastHit hit) => solver.CircleCast(origin, radius, moveDelta, out hit);

        public static bool BoxCast(Vector2 origin, Vector2 halfExtents, Vector2 moveDelta, out ShapeCastHit hit) => solver.BoxCast(origin, halfExtents, moveDelta, out hit);

        public static bool ShapeCast(KinematicBody body, Vector2 moveDelta, out ShapeCastHit hit) => solver.ShapeCast(body, moveDelta, out hit);
    }
}
