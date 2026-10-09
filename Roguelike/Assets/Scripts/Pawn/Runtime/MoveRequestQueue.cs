using Pawn.Data;
using Pawn.Interface;
using System.Collections.Generic;

namespace Pawn.Runtime
{
    internal sealed class MoveRequestQueue : IMoveRequestQueue
    {
        private readonly Queue<MoveRequest> moveRequests = new();

        internal IMoveRequestReceiver Receiver { get; }

        internal MoveRequestQueue()
        {
            Receiver = new RequestReceiver(moveRequests);
        }

        bool IMoveRequestQueue.TryDequeue(out MoveRequest moveRequest) => moveRequests.TryDequeue(out moveRequest);

        void IMoveRequestQueue.Clear() => moveRequests.Clear();

        private sealed class RequestReceiver : IMoveRequestReceiver
        {
            private readonly Queue<MoveRequest> queue;

            internal RequestReceiver(Queue<MoveRequest> queue)
            {
                this.queue = queue;
            }

            void IMoveRequestReceiver.Enqueue(MoveRequest moveRequest) => queue.Enqueue(moveRequest);
        }
    }
}
