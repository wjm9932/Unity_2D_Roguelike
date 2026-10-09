using Pawn.Data;

namespace Pawn.Interface
{
    public interface IMoveRequestQueue
    {
        bool TryDequeue(out MoveRequest moveRequest);
        void Clear();
    }
}
