using UnityEngine;

namespace Pawn.Interface
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

    public interface IPawnMovement : IPawnMover, IMoveRequestReceiver
    {
        void Dispose();
    }

    public interface IPawnMover
    {
        void OnMove(float dt);
    }

    public interface IMoveRequestReceiver
    {
        public void Enqueue(MoveRequest moveRequest);
    }
}
