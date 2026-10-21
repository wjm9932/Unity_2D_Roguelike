using Kinematic.Runtime;
using UnityEngine;

namespace Pawn.Runtime
{
    // 인게임 로직에 필요한 KinematicBody 기능만 제공하는 래퍼다.
    // 다형성이 필요하지 않고, 추상화를 통해 추가로 은닉하거나 접근을 제한할 기능도 없어 구체 클래스로 사용한다.
    internal sealed class PawnMotor
    {
        private readonly KinematicBody body;

        internal PawnMotor(KinematicBody body)
        {
            this.body = body;
        }

        internal void Move(Vector2 moveDelta) => body.Move(moveDelta);

        internal void Teleport(Vector2 position) => body.Teleport(position);
    }
}
