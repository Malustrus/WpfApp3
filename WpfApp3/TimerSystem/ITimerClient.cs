namespace WpfApp3.TimerSystem
{
    public interface ITimerClient
    {
        Guid Id { get; }
        void OnTick(DateTime currentTime);
    }
}