using Kinematic.Data;

namespace Kinematic.Runtime
{
    public static class KinematicWorld
    {
        private static readonly KinematicSolver solver = KinematicSolver.Instance;

        private static bool isInit = false;

        public static void Init(AABB bounds, bool useSweep)
        {
            if (isInit)
            {
                throw new System.Exception("Kinematic World is alreay initialized");
            }

            isInit = solver.Init(bounds, useSweep);
        }

        internal static void Tick()
        {
            if (isInit == false) return;

            solver.Solve();
        }

        public static void Register(KinematicBody body) => solver.Register(body);

        public static void Unregister(KinematicBody body) => solver.Unregister(body);

        public static void Dispose() => solver.Dispose();

#if UNITY_EDITOR
        internal static void DrawQuadTreeBounds() => solver.DrawQuadTreeBounds();
#endif
    }

#if UNITY_EDITOR
    internal sealed class KinematicWorldDebugDrawer : UnityEngine.MonoBehaviour
    {
        private static KinematicWorldDebugDrawer instance;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance != null)
            {
                return;
            }

            var drawerObject = new UnityEngine.GameObject(nameof(KinematicWorldDebugDrawer))
            {
                hideFlags = UnityEngine.HideFlags.DontSave
            };

            DontDestroyOnLoad(drawerObject);
            instance = drawerObject.AddComponent<KinematicWorldDebugDrawer>();
        }

        private void OnDrawGizmos()
        {
            if (UnityEngine.Application.isPlaying)
            {
                KinematicWorld.DrawQuadTreeBounds();
            }
        }
    }
#endif
}
