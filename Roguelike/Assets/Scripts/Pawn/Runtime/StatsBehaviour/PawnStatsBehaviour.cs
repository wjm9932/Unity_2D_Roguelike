using EventSystem;
using Pawn.Interface;

namespace Pawn.Runtime.StatsBehaviour
{
    public class PawnStatsBehaviour : IPawnStatsBehaviour, IEventListener
    {
        private IPawnStatsDefinition statsDefinition;

        public IPawnStatsDefinition StatsDefinition => statsDefinition;

        public PawnStatsBehaviour(IPawnStatsDefinition statsDefinition)
        {
            this.statsDefinition = statsDefinition;
        }

        public void OnUpdate(float dt)
        {
        }

        public bool OnEvent(Event e)
        {
            throw new System.NotImplementedException();
        }
    }
}
