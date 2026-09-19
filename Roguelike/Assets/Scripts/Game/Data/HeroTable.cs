using UnityEngine;
using UnityEngine.AddressableAssets;
using Pawn.Data;
using System.Collections.Generic;

public class HeroTable : ScriptableObject
{
    private const string DataPath = "hero_table";

    [SerializeField] private SerializableDictionary<int, AssetReferenceT<PawnDefinition>> table = new();

    public SerializableDictionary<int, AssetReferenceT<PawnDefinition>> Table => table;

    public static HeroTable Load()
    {
        var asset = Resources.Load<HeroTable>(DataPath);
        return asset ? Instantiate(asset) : CreateInstance<HeroTable>();
    }
}
