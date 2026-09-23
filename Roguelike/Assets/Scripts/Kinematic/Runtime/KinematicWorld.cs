using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    public static class KinematicWorld
    {
        private static readonly KinematicSolver solver = KinematicSolver.Instance;

        public static void Init(AABB bounds, bool useSweep) => solver.Init(bounds, useSweep);

        public static void Register(KinematicBody body) => solver.Register(body);

        public static void Unregister(KinematicBody body) => solver.Unregister(body);

        internal static void Tick() => solver.Solve();

#if UNITY_EDITOR
        internal static void DrawQuadTreeBounds() => solver.DrawQuadTreeBounds();
#endif
    }

#if UNITY_EDITOR
    internal sealed class KinematicWorldDebugDrawer : MonoBehaviour
    {
        private static KinematicWorldDebugDrawer instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance != null)
            {
                return;
            }

            var drawerObject = new GameObject(nameof(KinematicWorldDebugDrawer))
            {
                hideFlags = HideFlags.DontSave
            };

            DontDestroyOnLoad(drawerObject);
            instance = drawerObject.AddComponent<KinematicWorldDebugDrawer>();
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                KinematicWorld.DrawQuadTreeBounds();
            }
        }
    }
#endif
}
