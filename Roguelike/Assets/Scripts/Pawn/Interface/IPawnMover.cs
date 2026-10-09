using System;

namespace Pawn.Interface
{
    public interface IPawnMover
    {
        void OnMove(float dt);

        void Dispose();
    }
}
