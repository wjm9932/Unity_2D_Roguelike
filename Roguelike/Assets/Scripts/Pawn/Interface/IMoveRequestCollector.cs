using Pawn.Data;

namespace Pawn.Interface
{
    public interface IMoveRequestCollector
    {
        void Enqueue(MoveRequest moveRequest);
    }
}
