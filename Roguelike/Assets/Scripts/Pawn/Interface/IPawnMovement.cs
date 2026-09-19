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
        public Vector2 Vector { get; }

        public MoveRequest(MoveType moveType, Vector2 vector)
        {
            MoveType = moveType;
            Vector = vector;
        }
    }

    public interface IPawnMovement : IPawnMover, IMoveRequestReceiver
    {

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
