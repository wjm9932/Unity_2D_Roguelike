using System;
using UnityEngine;

namespace Pawn.Runtime
{
    internal class PawnAdapter : MonoBehaviour
    {
        private Action updatePawn;

        internal void Initialize(Action update) => updatePawn = update;

        private void Update() => updatePawn();
    }
}
