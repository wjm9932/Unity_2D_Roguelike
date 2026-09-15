using UnityEngine;

namespace Pawn.Data
{
    public interface IPawnDefinition
    {
        public PawnStatsDefinition PawnStatsDefinition { get; }
    }

    [CreateAssetMenu(fileName = "PawnDefinition", menuName = "Pawn/Data/PawnDefinition")]
    public class PawnDefinition : ScriptableObject, IPawnDefinition
    {
        [SerializeField] private PawnStatsDefinition pawnStatsDefinition;

        public PawnStatsDefinition PawnStatsDefinition => PawnStatsDefinition;
    }
}
