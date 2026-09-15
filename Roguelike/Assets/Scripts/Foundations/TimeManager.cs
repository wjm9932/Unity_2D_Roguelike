namespace Foundations
{
    public class TimeManager
    {
        public static TimeManager Instance => instance ??= new TimeManager();
        private static TimeManager instance;

        public float InGameDeltaTime => UnityEngine.Time.deltaTime;
    }
}
