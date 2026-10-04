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
            KinematicWorld.Init(new AABB(new Vector2(-10f, -10f), new Vector2(10f, 10f)), useSweep: false);

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