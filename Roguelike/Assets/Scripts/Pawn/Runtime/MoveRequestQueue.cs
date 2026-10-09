using Pawn.Data;
using Pawn.Interface;
using System.Collections.Generic;

namespace Pawn.Runtime
{
    internal sealed class MoveRequestQueue
    {
        private readonly Queue<MoveRequest> moveRequests = new();

        internal IMoveRequestReceiver Receiver { get; }

        internal MoveRequestQueue() => Receiver = new RequestReceiver(moveRequests);

        internal bool TryDequeue(out MoveRequest moveRequest) => moveRequests.TryDequeue(out moveRequest);

        internal void Clear() => moveRequests.Clear();

        private sealed class RequestReceiver : IMoveRequestReceiver
        {
            private readonly Queue<MoveRequest> queue;

            internal RequestReceiver(Queue<MoveRequest> queue) => this.queue = queue;

            public void Enqueue(MoveRequest moveRequest) => queue.Enqueue(moveRequest);
        }
    }
}
