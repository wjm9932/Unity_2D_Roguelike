using Cysharp.Threading.Tasks;
using Foundations;
using Game.Data;
using Pawn.Data;
using Pawn.Runtime;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Spawner
{
    public class PawnSpawner
    {
        private readonly DisposeLinker disposeLinker = new();
        private AsyncOperationHandle<PawnDefinition> pawnLoadHandle;

        public async UniTask Init()
        {
            // 임시로 0번만
            var assetRef = StaticDataRepository.Instance.GetAssetRefById(0);
            pawnLoadHandle = assetRef.LoadAssetAsync();

            await pawnLoadHandle;

            await PawnCreator.CreatePawn(pawnLoadHandle.Result, disposeLinker);
        }

        public void Dispose()
        {
            disposeLinker.Dispose();
            pawnLoadHandle.Release();
        }
    }

}
