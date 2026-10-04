using Kinematic.Data;
using Kinematic.Runtime.Spatial;
using System.Collections.Generic;

namespace Kinematic.Runtime
{
    internal sealed class KinematicSimulation
    {
        internal static KinematicSimulation Instance { get; } = new();
        private QuadTree staticBodies;
        private List<KinematicBody> dynamicBodies;
        private KinematicSolver solver;
        internal KinematicCast Cast { get; private set; }
        private bool isInit = false;

        internal void Init(in AABB bounds, bool useSweep)
        {
            if (isInit)
            {
                throw new System.Exception("Kinematic World is alreay initialized");
            }

            staticBodies = new QuadTree(bounds);
            dynamicBodies = new List<KinematicBody>();
            solver = new KinematicSolver(staticBodies, dynamicBodies, useSweep);
            Cast = new KinematicCast(staticBodies, dynamicBodies);
            isInit = true;
        }

        internal void Register(KinematicBody body)
        {
            if (body.IsStatic)
            {
                staticBodies.Insert(body);
            }
            else
            {
                dynamicBodies.Add(body);
            }
        }

        internal void Unregister(KinematicBody body)
        {
            if (body.IsStatic)
            {
                staticBodies.Remove(body);
            }
            else
            {
                dynamicBodies.Remove(body);
            }
        }

        internal void Tick()
        {
            if (isInit == false) return;

            solver.Solve();
        }

        // Dispose 구조 다시 한 번 더 봐야함
        internal void Dispose()
        {
            staticBodies.Clear();
            dynamicBodies.Clear();
            solver.Dispose();

            isInit = false;

            staticBodies = null;
            dynamicBodies = null;
            solver = null;
            Cast = null;
        }

#if UNITY_EDITOR
        internal void DrawQuadTreeBounds() => staticBodies?.DrawBounds(dynamicBodies);
#endif
    }
}
