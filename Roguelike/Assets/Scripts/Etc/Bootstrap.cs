using Cysharp.Threading.Tasks;
using Game.Data;
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

        await Game.Runtime.Game.Instance.ChangeScene(SceneId.Battle1);
    }
}
