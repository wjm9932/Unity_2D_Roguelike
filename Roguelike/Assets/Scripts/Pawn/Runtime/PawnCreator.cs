using Cysharp.Threading.Tasks;
using Pawn.Data;
using Pawn.Interface;
using Pawn.Runtime.Controller;
using Pawn.Runtime.StatsBehaviour;
using Pawn.Runtime.Behaviour;
using UnityEngine;
using Foundations;

namespace Pawn.Runtime
{
    public static class PawnCreator
    {
        public static async UniTask<IPawn> CreatePawn(PawnDefinition pawnDefinition, DisposeLinker disposeLinker)
        {
            var container = new GameObject(pawnDefinition.name);
            var adapter = container.AddComponent<PawnAdapter>();
         
            var rb = container.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            var avatar = Object.Instantiate(pawnDefinition.PawnAssetDefinition.Avatar, container.transform);
            avatar.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var movement = new PawnMovement(rb, pawnDefinition.PawnStatsDefinition.AccelerationCurve);
            var controller = new ManualPawnController();
            var statsBehaviour = new PawnStatsBehaviour(pawnDefinition.PawnStatsDefinition);
            var behaviour = new PawnBehaviour(adapter.transform, movement, pawnDefinition.PawnStatsDefinition);
            var pawn = new Pawn(controller, statsBehaviour, behaviour, movement);

            disposeLinker.Inject(pawn);
            adapter.Initialize(pawn.Update, pawn.Dispose);

            return pawn;
        }
    }
}