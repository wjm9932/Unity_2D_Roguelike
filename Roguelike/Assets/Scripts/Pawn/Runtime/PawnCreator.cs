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

            var avatar = Object.Instantiate(pawnDefinition.PawnAssetDefinition.Avatar, container.transform);
            avatar.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var controller = new ManualPawnController();
            var statsBehaviour = new PawnStatsBehaviour(pawnDefinition.PawnStatsDefinition);
            var movement = new PawnMovement(container.transform, pawnDefinition.PawnAssetDefinition.ColiderInfo, pawnDefinition.PawnStatsDefinition.AccelerationCurve);
            var behaviour = new PawnBehaviour(container.transform, movement, pawnDefinition.PawnStatsDefinition);
            var pawn = new Pawn(controller, statsBehaviour, behaviour, movement);

            var adapter = container.AddComponent<PawnAdapter>();
            adapter.Initialize(pawn.Update, pawn.Dispose);

            disposeLinker.Inject(pawn);

            return pawn;
        }
    }
}