using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    [DisallowMultipleComponent]
    public class KinematicBodyAdapter : MonoBehaviour
    {
        [SerializeField] private bool isStatic = true;
        [SerializeField] private ColliderInfo colliderInfo;

        private KinematicBody body;

        private void Awake()
        {
            if (body != null) return;

            body = new KinematicBody(transform, colliderInfo, isStatic);
        }

        private void OnEnable() => KinematicWorld.Register(body);

        private void OnDisable() => KinematicWorld.Unregister(body);

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.green;
    
            Vector3 center = transform.position + (Vector3)colliderInfo.Offset;

            switch (colliderInfo.Shape)
            {
                case Shape.Circle:
                    DrawCircle(center, Mathf.Max(0f, colliderInfo.Radius));
                    break;

                case Shape.Box:
                    Vector2 halfExtents = colliderInfo.HalfExtents;
                    Vector3 size = new(Mathf.Abs(halfExtents.x) * 2f, Mathf.Abs(halfExtents.y) * 2f,  0f);

                    Gizmos.DrawWireCube(center, size);
                    break;
            }

            Gizmos.color = previousColor;
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            if (radius <= 0f)
            {
                return;
            }

            var angleStep = Mathf.PI * 2f / 128;
            var previousPoint = center + Vector3.right * radius;

            for (int i = 1; i <= 128; i++)
            {
                var angle = angleStep * i;
                var nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);

                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
#endif
    }
}
