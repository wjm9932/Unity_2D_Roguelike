using Cysharp.Threading.Tasks;
using System.Threading;

public interface ISceneInitializer
{
    // 씬이 전환될 때 취소가 될 일이 있으려나?
    public UniTask Load(CancellationToken? token);

    public void Dispose();
}