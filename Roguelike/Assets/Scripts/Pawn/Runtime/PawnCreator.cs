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
            
            var avatar = Object.Instantiate(pawnDefinition.PawnAssetDefinition.Avatar, container.transform);
            avatar.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var controller = new ManualPawnController();
            var statsBehaviour = new PawnStatsBehaviour();
            var behaviour = new PawnBehaviour();
            var movement = new PawnMovement(rb, pawnDefinition.PawnStatsDefinition.AccelerationCurve);
            var pawn = new Pawn(controller, statsBehaviour, behaviour, movement);

            adapter.Initialize(pawn.Update);

            return pawn;
        }
    }
}