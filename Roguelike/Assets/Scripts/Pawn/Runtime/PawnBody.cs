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

        public ColliderInfo Shape => body.Shape;
        public Vector2 Center => body.Center;
        public int Layer => body.Layer;

        internal PawnBody(KinematicBody body)
        {
            this.body = body;
        }


        public void SetGhost(bool isGhost) => body.SetGhost(isGhost);

        public bool RayCast(Vector2 distance, int layerMask, out CastHit hit) => Kinematics.Raycast(body.Center, distance, body, layerMask, out hit);

        public bool ShapeCast(Vector2 distance, out CastHit hit) => ShapeCast(distance, ~0, out hit);

        public bool ShapeCast(Vector2 distance, int layerMask, out CastHit hit) => Kinematics.ShapeCast(body, distance, layerMask, out hit);

        public int ShapeCastAll(Vector2 distance, List<CastHit> results) => ShapeCastAll(distance, ~0, results);

        public int ShapeCastAll(Vector2 distance, int layerMask, List<CastHit> results) => Kinematics.ShapeCastAll(body, distance, layerMask, results);
    }
}
