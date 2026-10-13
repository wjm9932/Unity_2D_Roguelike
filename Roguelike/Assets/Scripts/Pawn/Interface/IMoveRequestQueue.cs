using Pawn.Data;
using System;

namespace Pawn.Interface
{
    public interface IMoveRequestQueue
    {
        bool TryDequeue(out MoveRequest moveRequest);

        void Clear();
    }
}
