using Pawn.Data;
using Pawn.Interface;
using System.Collections.Generic;

namespace Pawn.Runtime
{
    internal sealed class MoveRequestBuffer
    {
        private readonly Queue<MoveRequest> moveRequests = new();

        internal IMoveRequestQueue Queue { get; }
        internal IMoveRequestCollector Collector { get; }

        internal MoveRequestBuffer()
        {
            Queue = new RequestQueue(moveRequests);
            Collector = new MoveRequestCollector(moveRequests);
        }

        private sealed class RequestQueue : IMoveRequestQueue
        {
            private readonly Queue<MoveRequest> moveRequests;

            internal RequestQueue(Queue<MoveRequest> moveRequests)
            {
                this.moveRequests = moveRequests;
            }

            bool IMoveRequestQueue.TryDequeue(out MoveRequest moveRequest) => moveRequests.TryDequeue(out moveRequest);

            void IMoveRequestQueue.Clear() => moveRequests.Clear();
        }

        private sealed class MoveRequestCollector : IMoveRequestCollector
        {
            private readonly Queue<MoveRequest> moveRequests;

            internal MoveRequestCollector(Queue<MoveRequest> moveRequests)
            {
                this.moveRequests = moveRequests;
            }

            void IMoveRequestCollector.Enqueue(MoveRequest moveRequest) => moveRequests.Enqueue(moveRequest);
        }
    }
}
