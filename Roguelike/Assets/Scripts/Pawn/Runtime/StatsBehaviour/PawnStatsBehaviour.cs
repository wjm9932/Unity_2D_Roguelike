using Pawn.Interface;

namespace Pawn.Runtime.StatsBehaviour
{
    public class PawnStatsBehaviour : IPawnStatsBehaviour
    {
        private IPawnStatsDefinition statsDefinition;

        public IPawnStatsDefinition StatsDefinition => statsDefinition; 

    }
}
