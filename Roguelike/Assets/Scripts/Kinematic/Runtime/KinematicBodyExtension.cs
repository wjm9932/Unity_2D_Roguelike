using UnityEngine;

namespace Kinematic.Runtime
{
    internal static class KinematicBodyExtension
    {
        private static readonly int ghostLayerMask = LayerMask.GetMask("Pawn");

        internal static bool CanGhostThrough(this KinematicBody bodyA, KinematicBody bodyB) => (ghostLayerMask & (1 << bodyB.Layer)) != 0;

        internal static bool CanCollideWith(this KinematicBody bodyA, KinematicBody bodyB)
        {
            // 고스트 바디는 Pawn만 통과하며, 상대 바디의 충돌 반응도 함께 끈다.
            if ((bodyA.IsGhost && bodyA.CanGhostThrough(bodyB)) || (bodyB.IsGhost && bodyB.CanGhostThrough(bodyA)))
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
