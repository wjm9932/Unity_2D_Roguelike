using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Roguelike
{
    public sealed class UniTaskDelayExample : MonoBehaviour
    {
        private void Start()
        {
            WaitTenSecondsAsync().Forget();
        }

        private async UniTask WaitTenSecondsAsync()
        {
            Debug.Log("UniTask: 10초 대기를 시작합니다.");

            await UniTask.Delay(10000, cancellationToken: destroyCancellationToken);

            Debug.Log("UniTask: 10초 대기가 끝났습니다.");
        }
    }
}
