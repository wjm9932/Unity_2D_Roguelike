using Cysharp.Threading.Tasks;
using Pawn.Data;
using UnityEngine.AddressableAssets;

namespace Game.Data
{
    public class StaticDataRepository
    {
        private static StaticDataRepository instance;

        public static StaticDataRepository Instance => instance ??= new StaticDataRepository();

        private StaticDataRepository() { }

        private HeroTable heroTable;

        public static async UniTask LoadAsync()
        {
            await Instance.LoadInternalAsync();
        }

        private async UniTask LoadInternalAsync()
        {
            heroTable = HeroTable.Load();

            await UniTask.CompletedTask;
        }

        public AssetReferenceT<PawnDefinition> GetAssetRefById(int id) => heroTable.Table[id];
    }
}
