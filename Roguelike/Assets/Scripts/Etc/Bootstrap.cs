using Cysharp.Threading.Tasks;
using Game.Data;
using Kinematic.Data;
using Kinematic.Runtime;
using Scene;
using UnityEngine;

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        Game.Runtime.Game.Initialize(UniTask.Lazy(LoadAsync), SceneFactory);
    }

    private static ISceneInitializer SceneFactory(SceneId scene)
    {
        return scene switch
        {
            SceneId.Battle1 => new Stage(),
        };
    }

    // 현재 asmdef상 Game이 모든 Manager나 구현체를 아는것이 깔끔하지 않으며 순환참조가 발생할 수 있어 Bootstrap단으로 옮김
    private static async UniTask LoadAsync()
    {
        await StaticDataRepository.LoadAsync();

        // 이벤트로 Stage Loaded 이벤트 뿌리기 전까지만 여기서 초기화
        KinematicWorld.Init(new AABB(new Vector2(-10f, -10f), new Vector2(10f, 10f)), useSweep: false);

        await Game.Runtime.Game.Instance.ChangeScene(SceneId.Battle1);
    }
}
