using System;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal static class KinematicBodyExtension
    {
        internal static bool CanTriggerThrough(this KinematicBody bodyA, KinematicBody bodyB) => (bodyA.TriggerLayer & (1 << bodyB.Layer)) != 0;

        internal static bool IsTriggerPair(this KinematicBody bodyA, KinematicBody bodyB) 
            => (bodyA.IsTrigger && bodyA.CanTriggerThrough(bodyB)) || (bodyB.IsTrigger && bodyB.CanTriggerThrough(bodyA));

        // 레이어상 검사 가능한 쌍인지 판단한다. 트리거 여부는 접촉을 처리할 때 구분한다.
        internal static bool CanCollideWith(this KinematicBody bodyA, KinematicBody bodyB)
            => Physics2D.GetIgnoreLayerCollision(bodyA.Layer, bodyB.Layer) == false;

        // 기존 솔버의 후보 순회에서 트리거 접촉은 기록만 하고, 일반 접촉은 보정에 사용한다.
        internal static bool TryGetSolvingContact(this KinematicBody bodyA, KinematicBody bodyB, Action<KinematicBody, KinematicBody> onTriggerContact, out Contact contact)
        {
            contact = default;

            // 레이어 상 검사 가능한 쌍인지 판단한다.
            if (bodyA.CanCollideWith(bodyB) == false)
            {
                return false;
            }

            if (bodyA.IsTriggerPair(bodyB))
            {
                if (KinematicCollision.TryGetContact(bodyA.Center, bodyA.Shape, bodyB.Center, bodyB.Shape, out contact))
                {
                    onTriggerContact(bodyA, bodyB);
                }

                return false;
            }

            return bodyA.TryCollide(bodyB, out contact);
        }

        internal static bool TryCollide(this KinematicBody bodyA, KinematicBody bodyB, out Contact contact)
        {
            return KinematicCollision.TryCollide(bodyA.Center, bodyA.Shape, bodyB.Center, bodyB.Shape, out contact);
        }
    }
}
