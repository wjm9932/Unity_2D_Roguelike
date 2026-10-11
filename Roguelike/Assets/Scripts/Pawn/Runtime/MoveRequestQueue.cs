using Pawn.Data;
using Pawn.Interface;
using System.Collections.Generic;

namespace Pawn.Runtime
{
    internal sealed class MoveRequestQueue : IMoveRequestQueue
    {
        private readonly Queue<MoveRequest> moveRequests = new();

        internal IMoveRequestCollector Collector { get; }

        internal MoveRequestQueue()
        {
            Collector = new RequestCollector(moveRequests);
        }

        bool IMoveRequestQueue.TryDequeue(out MoveRequest moveRequest) => moveRequests.TryDequeue(out moveRequest);

        void IMoveRequestQueue.Clear() => moveRequests.Clear();

        private sealed class RequestCollector : IMoveRequestCollector
        {
            private readonly Queue<MoveRequest> queue;

            internal RequestCollector(Queue<MoveRequest> queue)
            {
                this.queue = queue;
            }

            void IMoveRequestCollector.Enqueue(MoveRequest moveRequest) => queue.Enqueue(moveRequest);
        }
    }
}
