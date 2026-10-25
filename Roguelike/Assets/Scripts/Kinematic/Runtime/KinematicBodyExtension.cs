using UnityEngine;

namespace Kinematic.Runtime
{
    internal static class KinematicBodyExtension
    {
        internal static bool CanTriggerThrough(this KinematicBody bodyA, KinematicBody bodyB) => (bodyA.TriggerLayer & (1 << bodyB.Layer)) != 0;

        internal static bool CanCollideWith(this KinematicBody bodyA, KinematicBody bodyB)
        {
            if ((bodyA.IsTrigger && bodyA.CanTriggerThrough(bodyB)) || (bodyB.IsTrigger && bodyB.CanTriggerThrough(bodyA)))
            {
                return false;
            }

            // 2D Layer Collision Matrix에서 충돌이 허용된 레이어 쌍만 양쪽 모두 반응한다.
            return Physics2D.GetIgnoreLayerCollision(bodyA.Layer, bodyB.Layer) == false;
        }

        internal static bool TryCollide(this KinematicBody bodyA, KinematicBody bodyB, out Contact contact)
        {
            return KinematicCollision.TryCollide(bodyA.Center, bodyA.Shape, bodyB.Center, bodyB.Shape, out contact);
        }
    }
}
