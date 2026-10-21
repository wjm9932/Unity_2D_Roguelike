using CustomAttribute.Runtime;
using UnityEngine;

namespace Kinematic.Data
{
    public enum OverlapResolutionMode
    {
        Instant,
        Smooth
    }

    public enum CollisionDetection
    {
        Discrete,
        Sweep
    }

    [CreateAssetMenu(fileName = "KinematicSettings", menuName = "Kinematic/Settings", order = 1)]
    public class KinematicSettings : ScriptableObject
    {
        private const string DataPath = "kinematic_settings";

        private static KinematicSettings defaultSettings;
        // 기본 세팅 값
        private static KinematicSettings Default
        {
            get
            {
                if (defaultSettings == null)
                {
                    defaultSettings = CreateInstance<KinematicSettings>();
                    defaultSettings.collisionDetection = CollisionDetection.Discrete;
                    defaultSettings.maxSolverIterationCount = 16;
                    defaultSettings.overlapResolutionMode = OverlapResolutionMode.Instant;
                }

                return defaultSettings;
            }
        }

        // 충돌 감지 방식
        [SerializeField] private CollisionDetection collisionDetection;
        // Discrete 모드에서 겹침 해소를 반복할 최대 횟수
        [ShowIf(nameof(collisionDetection), (int)CollisionDetection.Discrete)]
        [SerializeField] private int maxSolverIterationCount = 16;
        // 겹침 발생 시 위치 보정 방식
        [SerializeField] private OverlapResolutionMode overlapResolutionMode;

        public CollisionDetection CollisionDetection => collisionDetection;
        public int MaxSolverIterationCount => maxSolverIterationCount;
        public OverlapResolutionMode OverlapResolutionMode => overlapResolutionMode;

        public static KinematicSettings Load()
        {
            var asset = Resources.Load<KinematicSettings>(DataPath);

            return asset ? Instantiate(asset) : Default;
        }
    }
}
