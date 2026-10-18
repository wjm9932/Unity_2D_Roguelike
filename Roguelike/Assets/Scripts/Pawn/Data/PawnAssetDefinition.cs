using Kinematic.Data;
using System;
using UnityEngine;

namespace Pawn.Data
{
    public interface IPawnAssetDefinition
    {
    }

    [Serializable]
    public class PawnAssetDefinition : IPawnAssetDefinition
    {
        [SerializeField] private GameObject avatar;
        [SerializeField] private ColliderInfo colliderInfo;
        [SerializeField] private MovePriority movePriority = MovePriority.Medium;

        public GameObject Avatar => avatar;
        public ColliderInfo ColiderInfo => colliderInfo;
        public MovePriority MovePriority => movePriority;
    }
}
