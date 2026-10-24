using Kinematic.Data;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GameLoop")]

namespace Kinematic.Runtime
{
    public static class KinematicWorld
    {
        internal static KinematicSettings Settings { get; private set; }

        public static void Init(in AABB bounds)
        {
            if (Settings == null) 
            {
                Settings = KinematicSettings.Load();
            }

            KinematicSimulation.Instance.Init(bounds);
        }

        public static void Register(KinematicBody body) => KinematicSimulation.Instance.Register(body);

        public static void Unregister(KinematicBody body) => KinematicSimulation.Instance.Unregister(body);

        internal static void Tick() => KinematicSimulation.Instance.Tick();

        public static void Dispose() => KinematicSimulation.Instance.Dispose();

#if UNITY_EDITOR
        internal static void DrawQuadTreeBounds() => KinematicSimulation.Instance.DrawQuadTreeBounds();
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
