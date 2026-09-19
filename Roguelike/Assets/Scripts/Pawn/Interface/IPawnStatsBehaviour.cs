namespace Pawn.Interface
{
    public interface IPawnStatsBehaviour
    {
        public IPawnStatsDefinition StatsDefinition { get; }

        public void OnUpdate(float dt);
    }
}
