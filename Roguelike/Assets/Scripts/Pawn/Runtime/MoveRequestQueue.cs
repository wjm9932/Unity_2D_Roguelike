using Pawn.Data;
using Pawn.Interface;
using System.Collections.Generic;

namespace Pawn.Runtime
{
    // 요청 큐를 만들고, 소비하는 쪽에는 꺼내기와 비우기 기능을 제공한다.
    internal sealed class MoveRequestQueue : IMoveRequestQueue
    {
        // 요청을 FIFO 순서로 보관하는 실제 저장소다.
        private readonly Queue<MoveRequest> moveRequests = new();

        // Queue와 Collector가 항상 함께 연결되도록 Create를 통해서만 생성한다.
        private MoveRequestQueue() {}

        // 가장 먼저 들어온 요청을 꺼내고 저장소에서 제거한다. 요청이 없으면 false를 반환한다.
        bool IMoveRequestQueue.TryDequeue(out MoveRequest moveRequest) => moveRequests.TryDequeue(out moveRequest);

        // 아직 소비하지 않은 요청을 모두 비운다.
        public void Clear() => moveRequests.Clear();

        // 이 Queue의 저장소에 요청을 넣는 전용 객체다. 구체 타입은 외부에 노출하지 않는다.
        private sealed class MoveRequestCollector : IMoveRequestCollector
        {
            // 별도 저장소를 만들지 않고, 바깥 Queue와 같은 저장소를 참조한다.
            private readonly Queue<MoveRequest> moveRequests;

            // 요청을 넣을 저장소를 생성하는 쪽에서 전달받는다.
            internal MoveRequestCollector(Queue<MoveRequest> moveRequests)
            {
                // 전달받은 저장소의 참조를 보관한다. 요청이나 저장소를 복사하지 않는다.
                this.moveRequests = moveRequests;
            }

            // 생산하는 쪽에는 요청 추가 기능만 제공한다.
            void IMoveRequestCollector.Enqueue(MoveRequest moveRequest) => moveRequests.Enqueue(moveRequest);
        }
        
        // 같은 저장소에 연결된 소비용 Queue와 추가용 Collector를 함께 생성한다.
        internal static (IMoveRequestQueue, IMoveRequestCollector) Create()
        {
            // Queue를 생성하면서 실제 요청 저장소도 만든다.
            var queue = new MoveRequestQueue();
            // Queue가 만든 저장소를 공유하는 별도의 Collector 객체를 만든다.
            var collector = new MoveRequestCollector(queue.moveRequests);

            // 호출하는 쪽에서 각 역할에 맞게 전달하도록 두 객체를 반환한다. Queue는 Collector를 보관하지 않는다.
            return (queue, collector);
        }
    }
}
