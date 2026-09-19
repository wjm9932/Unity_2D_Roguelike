using System;
using UnityEngine;

namespace Pawn.Runtime
{
    internal class PawnAdapter : MonoBehaviour
    {
        private Action updatePawn;

        private Action disposePawn;

        internal void Initialize(Action update, Action dispose)
        {
            updatePawn = update;
            disposePawn = dispose;
        }

        private void Update() => updatePawn();

        private void OnDestroy() => disposePawn();
    }
}
