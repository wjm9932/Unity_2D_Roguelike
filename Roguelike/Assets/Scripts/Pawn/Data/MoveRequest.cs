using UnityEngine;

namespace Pawn.Data
{
    public enum MoveType
    {
        Active,
        Passive,
        Teleport
    }

    public struct MoveRequest
    {
        public MoveType MoveType { get; }
        public Vector2 Velocity { get; }

        public MoveRequest(MoveType moveType, Vector2 velocity)
        {
            MoveType = moveType;
            Velocity = velocity;
        }
    }
}
