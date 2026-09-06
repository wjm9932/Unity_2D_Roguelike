namespace Foundations
{
    public class TimeManager
    {
        public static TimeManager Instance => instance ??= new TimeManager();
        private static TimeManager instance;

        public float DeltaTime => UnityEngine.Time.deltaTime;
    }
}
