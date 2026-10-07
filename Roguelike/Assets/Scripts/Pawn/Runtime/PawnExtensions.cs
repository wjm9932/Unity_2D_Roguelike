using EventSystem;
using System;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GameLoop")]

namespace Pawn.Runtime
{
    internal static class PawnExtensions
    {
        private static event Action consumeMoveRequests;

        internal static void RegisterConsumeMoveRequest(this Pawn pawn)
        {
            consumeMoveRequests += pawn.ConsumeMoveRequest;
        }

        internal static void UnregisterConsumeMoveRequest(this Pawn pawn)
        {
            consumeMoveRequests -= pawn.ConsumeMoveRequest;
        }

        internal static void ConsumeMoveRequests() => consumeMoveRequests?.Invoke();

        internal static void ThrowUpdate(this Pawn pawn, float dt)
        {
            pawn.PawnController.OnUpdate(dt);

            pawn.PawnStatsBehaviour.OnUpdate(dt);

            pawn.PawnBehaviour.OnUpdate(dt);
        }

        internal static bool ThrowEvent(this Pawn pawn, Event e)
        {
            if ((pawn.PawnController as IEventListener)?.OnEvent(e) ?? false)
            {
                return true;
            }

            if ((pawn.PawnStatsBehaviour as IEventListener)?.OnEvent(e) ?? false)
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
