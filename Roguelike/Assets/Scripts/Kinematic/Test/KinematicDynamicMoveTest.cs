using Input;
using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    [DisallowMultipleComponent]
    public sealed class KinematicDynamicMoveTest : MonoBehaviour
    {
        [SerializeField] private ColliderInfo colliderInfo = ColliderInfo.CreateCircle(Vector2.zero, 0.5f);

        private KinematicBody body;

        private void OnEnable()
        {
            body = new KinematicBody(transform, colliderInfo, isStatic: false);
            KinematicWorld.Register(body);
        }

        private void Update()
        {
            var pawnControl = InputManager.Instance.PawnControls;
            body.Move(pawnControl.TestMove.CurrentValue.RawValue * 4.5f * Time.deltaTime);
        }

        private void OnDisable()
        {
            KinematicWorld.Unregister(body);
            body = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var center = transform.position + (Vector3)colliderInfo.Offset;
            if (colliderInfo.Shape == Shape.Circle)
            {
                Gizmos.DrawWireSphere(center, colliderInfo.Radius);
            }
            else
            {
                Gizmos.DrawWireCube(center, new Vector3(colliderInfo.HalfExtents.x * 2f, colliderInfo.HalfExtents.y * 2f, 0f));
            }
        }
#endif
    }
}
