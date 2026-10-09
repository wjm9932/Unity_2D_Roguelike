using Cysharp.Threading.Tasks;
using Kinematic.Data;
using Kinematic.Runtime;
using Spawner;
using System.Threading;
using UnityEngine;

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
            KinematicWorld.Dispose();
        }
    }
}