namespace Pawn
{
    internal static class PawnExtensions
    {
        internal static void OnUpdate(this Pawn pawn, float dt)
        {
            pawn.PawnController.ThrowUpdate(dt);
        }
    }
}
