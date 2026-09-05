namespace EventSystem
{
    public interface IEventListener
    {
        public bool OnEvent(Event e);
    }
}
