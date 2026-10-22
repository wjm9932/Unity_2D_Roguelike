using System;

namespace Pawn.Interface
{
    public interface IPawnMoveProcessor
    {
        void OnMove(float dt);

        void Dispose();
    }
}
