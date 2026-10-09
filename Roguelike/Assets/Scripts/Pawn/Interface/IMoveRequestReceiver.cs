using Pawn.Data;

namespace Pawn.Interface
{
    public interface IMoveRequestReceiver
    {
        void Enqueue(MoveRequest moveRequest);
    }
}
