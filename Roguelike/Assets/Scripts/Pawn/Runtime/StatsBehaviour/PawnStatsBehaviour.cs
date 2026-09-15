using Pawn.Interface;

namespace Pawn.Runtime.StatsBehaviour
{
    internal class PawnStatsBehaviour : IPawnStatsBehaviour
    {
        private IPawnStatsDefinition statsDefinition;

        public IPawnStatsDefinition StatsDefinition => statsDefinition; 
    }
}
