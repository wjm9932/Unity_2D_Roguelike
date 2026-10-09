using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal sealed class KinematicGhostState
    {
        private static readonly int ghostLayerMask = LayerMask.GetMask("Pawn");
        private readonly KinematicBody body;
        private readonly HashSet<KinematicBody> ghostOverlaps = new();

        internal bool IsGhost { get; private set; }
        internal bool NeedsGhostOverlapCheck { get; private set; }
        internal bool HasGhostOverlaps => ghostOverlaps.Count > 0;

        internal KinematicGhostState(KinematicBody body)
        {
            this.body = body;
        }

        internal void SetGhost(bool isGhost)
        {
            if (IsGhost == isGhost) return;

            // 복귀할 때 이미 겹친 Pawn만 기록하고, 위치 보정 없이 직접 빠져나오도록 한다.
            NeedsGhostOverlapCheck = !isGhost;
            IsGhost = isGhost;
        }

        internal bool CanGhostThrough(KinematicBody other) => (ghostLayerMask & (1 << other.Layer)) != 0;

        internal void TrackGhostOverlap(KinematicBody other) => ghostOverlaps.Add(other);

        internal bool HasGhostOverlap(KinematicBody other) => ghostOverlaps.Contains(other) || other.Ghost.ghostOverlaps.Contains(body);

        internal void CompleteGhostOverlapCheck() => NeedsGhostOverlapCheck = false;

        internal void TryRemoveSeparatedGhostOverlaps() => ghostOverlaps.RemoveWhere(ShouldRemoveGhostOverlap);

        internal void TryRemoveGhostOverlap(KinematicBody other) => ghostOverlaps.Remove(other);

        private bool ShouldRemoveGhostOverlap(KinematicBody other) => IsGhost || other.Ghost.IsGhost || body.CanCollideWith(other) == false || body.TryCollide(other, out _) == false;

        internal void ClearGhostOverlaps() => ghostOverlaps.Clear();
    }
}
