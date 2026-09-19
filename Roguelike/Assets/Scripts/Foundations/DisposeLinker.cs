using System;
using System.Collections.Generic;

namespace Foundations
{
    public class DisposeLinker
    {
        /// <summary>
        /// 캐시 히트율을 조금 포기하고 중복 추가를 방지해 예상치 런타임 오류를 방지한다.
        /// </summary>
        private readonly HashSet<IDisposable> disposables = new();

        public void Inject(IDisposable disposable) => disposables.Add(disposable);

        public void Dispose()
        {
            foreach(var disposable in disposables)
            {
                disposable.Dispose();
            }

            disposables.Clear();
        }
    }
}
