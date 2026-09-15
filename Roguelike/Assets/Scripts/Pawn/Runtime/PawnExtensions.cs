using EventSystem;

namespace Pawn.Runtime 
{ 
    internal static class PawnExtensions
    {
        internal static void ThrowUpdate(this Pawn pawn, float dt)
        {
            pawn.PawnController.OnUpdate(dt);

            pawn.PawnBehaviour.OnUpdate(dt);


        }

        internal static bool ThrowEvent(this Pawn pawn, Event e)
        {
            if ((pawn.PawnController as IEventListener)?.OnEvent(e) ?? false)
            {
                return true;
            }

            if ((pawn.PawnBehaviour as IEventListener)?.OnEvent(e) ?? false)
            {
                return true;
            }

            return false;
        }
    }
}
