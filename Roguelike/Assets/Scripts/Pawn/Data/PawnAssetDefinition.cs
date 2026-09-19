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

        public GameObject Avatar => avatar;
    }
}
