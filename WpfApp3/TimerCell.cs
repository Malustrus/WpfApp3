using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Media;
using WpfApp3.TimerSystem;

namespace WpfApp3
{
    public partial class TimerCell : ObservableObject, ITimerClient, IDisposable
    {
        // Propriétés observables
        [ObservableProperty] private TimerState state;
        [ObservableProperty] private bool isClickable = true;
        [ObservableProperty] private string elapsedText = "00:00:00";
        [ObservableProperty] private Brush color;
        [ObservableProperty] private string label;
        [ObservableProperty] private DateTime lastReadyTime = DateTime.MinValue;

        // Propriétés publiques
        public TimerConfiguration Configuration { get; }
        public Guid Id { get; } = Guid.NewGuid();

        // État interne
        private DateTime _startTime;
        private bool _isRunning;
        private bool _isDisposed;
        private TimerMode _timerMode;
        private TimerState oldState;

        // Événements
        public event EventHandler<TimerStateChangedEventArgs> StateChanged;
        public event EventHandler<TransferEventArgs> TransferRequested;

        // Commandes
        public RelayCommand ToggleCommand { get; }
        public RelayCommand RequestTransferCommand { get; }

        public TimerCell(TimerConfiguration config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));
            
            // Initialisation des commandes
            ToggleCommand = new RelayCommand(OnToggle);
            RequestTransferCommand = new RelayCommand(OnRequestTransfer);
            
            Reset();
        }

        // Méthode appelée par le TimerManager
        public void OnTick(DateTime currentTime)
        {
            if (!_isRunning) return;

            var elapsed = currentTime - _startTime;
            
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateState(elapsed);
                UpdateElapsedText(elapsed);
            });
        }

        // Gestion des états
        private void UpdateState(TimeSpan elapsed)
        {
       
            switch (State)
            {
                case TimerState.Heating when elapsed >= Configuration.HeatingDuration && Configuration.CoolingDuration == TimeSpan.Zero:
                    State = TimerState.HeatingDone;                  
                    Color = Configuration.HeatingDoneColor;
                    break;
                case TimerState.Heating when elapsed >= Configuration.HeatingDuration && Configuration.CoolingDuration > TimeSpan.Zero:
                    State = TimerState.WaitingForCooling;
                    Color = Configuration.WaitingForCoolingColor;
                    break;
                case TimerState.HeatingDone when Configuration.MaxHeatTime > TimeSpan.Zero && elapsed >= Configuration.MaxHeatTime :
                    State = TimerState.HeatTimeExceeded;                  
                    Color = Configuration.HeatExceededColor;
                    break;
                case TimerState.WaitingForCooling when Configuration.MaxHeatTime > TimeSpan.Zero && elapsed >= Configuration.MaxHeatTime:
                    State = TimerState.HeatTimeExceeded;
                    Color = Configuration.HeatExceededColor;
                    break;
                case TimerState.Cooling when elapsed >= Configuration.CoolingDuration:
                    State = TimerState.CoolingDone;
                    Color = Configuration.CoolingDoneColor;
                    break;
                case TimerState.CoolingDone when Configuration.MaxCoolingTime > TimeSpan.Zero && elapsed >= Configuration.MaxCoolingTime:
                    State = TimerState.CoolingTimeExceeded;
                    Color = Configuration.CoolingExceededColor;
                    break;
            }

            if (oldState != State)
            {
                oldState = State;
                Label = State.ToString();
                _startTime = DateTime.Now;
                StateChanged?.Invoke(this, new TimerStateChangedEventArgs(oldState, State));
                System.Diagnostics.Debug.WriteLine(State);
            }
        }

        // Méthodes publiques
        public void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(TimerCell));
            
            _startTime = DateTime.Now;
            _isRunning = true;
            TimerManager.Instance.Register(this);
            
            State = TimerState.Heating;
            Color = Configuration.HeatingColor;
        }

        public void Reset()
        {
            _isRunning = false;
            TimerManager.Instance.Unregister(this);
            State = TimerState.Idle;
            Label = State.ToString();
            Color = Configuration.IdleColor;
            UpdateElapsedText(Configuration.HeatingDuration);
            LastReadyTime = DateTime.Now;
        }

        // Méthodes privées
        private void OnToggle()
        {
            switch (State)
            {
                case TimerState.Idle:
                    Start();
                    break;
                case TimerState.Heating:
                case TimerState.HeatingDone:
                case TimerState.HeatTimeExceeded:
                case TimerState.Cooling:
                case TimerState.CoolingDone:
                case TimerState.CoolingTimeExceeded:
                    Reset();
                    break;
                case TimerState.WaitingForCooling:
                    State = TimerState.Cooling;
                    break;
            }
            UpdateState(elapsed);
        }

        private void OnRequestTransfer()
        {
            TransferRequested?.Invoke(this, new TransferEventArgs { Source = this });
        }

        private void UpdateElapsedText(TimeSpan elapsed)
        {
            ElapsedText = elapsed.ToString(@"hh\:mm\:ss");
        }

        // IDisposable
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            Reset();
            StateChanged = null;
            TransferRequested = null;
        }
    }

    public class TransferEventArgs : EventArgs
    {
        public TimerCell? Source { get; set; }
        public TimerCell? Destination { get; set; }
    }

    public enum TimerState
    {
        Idle,               // Prêt
        Heating,            // En cours
        HeatingDone,
        Cooling,            // Refroidissement
        CoolingDone,
        WaitingForCooling,     // En attente de refroidissement
        HeatTimeExceeded,   // Temps de chauffe dépassé
        CoolingTimeExceeded // Temps de refroidissement dépassé
    }

    public enum TimerMode
    {
        CountUp,     // 0 → durée
        CountDown    // durée → 0
    }

    public class TimerStateChangedEventArgs : EventArgs
    {
        public TimerState OldState { get; }
        public TimerState NewState { get; }
        public DateTime TransitionTime { get; }
        public TimerStateChangedEventArgs(TimerState oldState, TimerState newState)
        {
            OldState = oldState;
            NewState = newState;
            TransitionTime = DateTime.Now;
        }
    }
}
