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
        [SerializeField] private LayerMask collidableLayer = ~0;

        public GameObject Avatar => avatar;
        public ColliderInfo ColiderInfo => colliderInfo;
        public LayerMask CollidableLayer => collidableLayer;
    }
}
