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
        private static KinematicSettings defaultSettings;
        // 기본 세팅 값
        public static KinematicSettings Default
        {
            get
            {
                if (defaultSettings == null)
                {
                    defaultSettings = CreateInstance<KinematicSettings>();
                    defaultSettings.collisionDetection = CollisionDetection.Discrete;
                    defaultSettings.overlapResolutionMode = OverlapResolutionMode.Instant;
                    defaultSettings.maxSolverIterationCount = 16;
                }

                return defaultSettings;
            }
        }

        // 충돌 감지 방식
        [SerializeField] private CollisionDetection collisionDetection;
        // 겹침 발생 시 위치 보정 방식
        [SerializeField] private OverlapResolutionMode overlapResolutionMode;
        // Discrete 모드에서 겹침 해소를 반복할 최대 횟수
        [ShowIf(nameof(collisionDetection), (int)CollisionDetection.Discrete)]
        [SerializeField] private int maxSolverIterationCount = 16;

        public CollisionDetection CollisionDetection => collisionDetection;
        public OverlapResolutionMode OverlapResolutionMode => overlapResolutionMode;
        public int MaxSolverIterationCount => maxSolverIterationCount;
    }
}
