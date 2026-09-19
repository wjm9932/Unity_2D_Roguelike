using UnityEngine;

namespace Pawn.Data
{
    public interface IPawnDefinition
    {
        public PawnAssetDefinition PawnAssetDefinition { get; }
        public PawnStatsDefinition PawnStatsDefinition { get; }
    }

    [CreateAssetMenu(fileName = "PawnDefinition", menuName = "Pawn/Data/PawnDefinition")]
    public class PawnDefinition : ScriptableObject, IPawnDefinition
    {
        [SerializeField] private PawnAssetDefinition pawnAssetDefinition;
        [SerializeField] private PawnStatsDefinition pawnStatsDefinition;

        public PawnAssetDefinition PawnAssetDefinition => pawnAssetDefinition;
        public PawnStatsDefinition PawnStatsDefinition => pawnStatsDefinition;
    }
}
