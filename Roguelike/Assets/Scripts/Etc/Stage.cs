using Cysharp.Threading.Tasks;
using Spawner;
using System.Threading;

namespace Scene
{
    public class Stage : ISceneInitializer
    {
        private PawnSpawner pawnSpawner;

        public async UniTask Load(CancellationToken? token)
        {
            pawnSpawner = new PawnSpawner();

            await pawnSpawner.Init();
        }

        public void Dispose()
        {
            pawnSpawner.Dispose();
        }
    }
}