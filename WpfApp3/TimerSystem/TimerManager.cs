using System.Collections.Concurrent;

namespace WpfApp3.TimerSystem
{
    public sealed class TimerManager : IDisposable
    {
        private static readonly Lazy<TimerManager> _instance = new(() => new TimerManager());
        private readonly System.Timers.Timer _masterTimer;
        private readonly ConcurrentDictionary<Guid, ITimerClient> _clients;
        private bool _isDisposed;

        public static TimerManager Instance => _instance.Value;

        private TimerManager()
        {
            _clients = new ConcurrentDictionary<Guid, ITimerClient>();
            _masterTimer = new System.Timers.Timer(50);
            _masterTimer.Elapsed += OnTimerElapsed;
            _masterTimer.Start();
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var client in _clients.Values)
            {
                client.OnTick(e.SignalTime);
            }
        }

        public void Register(ITimerClient client)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(TimerManager));
            _clients.TryAdd(client.Id, client);
        }

        public void Unregister(ITimerClient client)
        {
            _clients.TryRemove(client.Id, out _);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            _masterTimer.Stop();
            _masterTimer.Elapsed -= OnTimerElapsed;
            _masterTimer.Dispose();
            
            _clients.Clear();
        }
    }
}