using UnityEngine;
using Pawn.Data;
using UnityEngine.AddressableAssets;

public struct HeroData
{
    public AssetReferenceT<PawnDefinition> CharacterDefinition;
}

public class HeroTable : ScriptableObject
{
}
