using Kinematic.Data;
using Kinematic.Runtime;
using System.Collections.Generic;
using UnityEngine;

namespace Pawn.Runtime
{
    // 인게임 로직에 필요한 KinematicBody 기능만 제공하는 래퍼다.
    // 다형성이 필요하지 않고, 추상화를 통해 추가로 은닉하거나 접근을 제한할 기능도 없어 구체 클래스로 사용한다.
    public sealed class PawnBody
    {
        private readonly KinematicBody body;

        public Transform Transform => body.Transform;
        public ColliderInfo Shape => body.Shape;
        public Vector2 Center => body.Center;
        public int Layer => body.Layer;

        public static readonly int IgnoreLayer = LayerMask.GetMask("Pawn");
        private int triggerLayer;

        internal PawnBody(KinematicBody body)
        {
            this.body = body;
        }

        public void SetGhost(bool isGhost)
        {
            if (isGhost)
            {
                triggerLayer = body.TriggerLayer;

                body.TriggerLayer = LayerMask.GetMask("Pawn");
            }
            else
            {
                body.TriggerLayer = triggerLayer;
            }

            body.SetTrigger(isGhost);
        }

        public void SetTrigger(bool isTrigger) => body.SetTrigger(isTrigger);


        #region ShapeCast
        public bool ShapeCast(Vector2 direction, float distance, out CastHit hit)
            => ShapeCast(direction, distance, ~0, out hit);

        public bool ShapeCast(Vector2 direction, float distance, int layerMask, out CastHit hit)
            => Kinematics.ShapeCast(body, direction, distance, layerMask & ~IgnoreLayer, out hit);

        public int ShapeCastAll(Vector2 direction, float distance, List<CastHit> hits)
            => ShapeCastAll(direction, distance, ~0, hits);

        public int ShapeCastAll(Vector2 direction, float distance, int layerMask, List<CastHit> hits)
            => Kinematics.ShapeCastAll(body, direction, distance, layerMask & ~IgnoreLayer, hits);
        #endregion
    }
}
