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
            var moveRequests = new MoveRequestQueue();
            var mover = new PawnMover(container.transform, pawnDefinition.PawnAssetDefinition.ColiderInfo, pawnDefinition.PawnStatsDefinition.AccelerationCurve, moveRequests);
            var behaviour = new PawnBehaviour(container.transform, moveRequests.Receiver, pawnDefinition.PawnStatsDefinition);
            var pawn = new Pawn(controller, statsBehaviour, behaviour, mover);

            // 이 어뎁터 구조는 고민 필요
            var adapter = container.AddComponent<PawnAdapter>();
            adapter.Initialize(pawn.Update);

            disposeLinker.Inject(pawn);

            return pawn;
        }
    }
}
