using UnityEngine;
using Pawn.Data;
using UnityEngine.AddressableAssets;
using System;

[Serializable]
public struct HeroData
{
    public AssetReferenceT<PawnDefinition> CharacterDefinition;
}

public class HeroTable : ScriptableObject
{
}
