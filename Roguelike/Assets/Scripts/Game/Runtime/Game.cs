using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

public enum SceneId 
{
    MainMenu, 
    Battle1, 
    Battle2,
    Battle3 
}

namespace Game.Runtime
{
    public class Game
    {
        public static Game Instance { get; private set; }

        // 다른 곳에서 초기화 await가 필요할 수 있으니 AysncLayz로 선언
        public AsyncLazy InitializeTask { get; private set; }

        private Func<SceneId, ISceneInitializer> sceneInitializerFactory;

        private ISceneInitializer sceneInitializer;

        public static void Initialize(AsyncLazy initializeTask, Func<SceneId, ISceneInitializer> sceneInitializerFactory)
        {
            Physics2D.simulationMode = SimulationMode2D.Update;

            Instance = new Game
            {
                InitializeTask = initializeTask,
                sceneInitializerFactory = sceneInitializerFactory
            };

            // 초기화와 관련 없는 작업은 멈추게 하지 않게하기 위해 Fire and Forget
            Instance.InitializeTask.Task.Forget();
        }

        public async UniTask ChangeScene(SceneId nextScene)
        {
            //씬 로드
            var operation = SceneManager.LoadSceneAsync((int)nextScene).ToUniTask();
            await operation;

            sceneInitializer?.Dispose();
            sceneInitializer = sceneInitializerFactory.Invoke(nextScene);
            await sceneInitializer.Load(null);
        }
    }

}
