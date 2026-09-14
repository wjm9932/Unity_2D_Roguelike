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
        public MoveType MoveType { get; private set; }
        public Vector2 Vector { get; private set; }
    }

    public interface IPawnMovement : IPawnMover, IMoveRequestReceiver
    {
  
    }

    public interface IPawnMover
    {
        public void OnMove(float dt);
    }

    public interface IMoveRequestReceiver
    {
        public void Enqueue(MoveRequest moveRequest);
    }
}
