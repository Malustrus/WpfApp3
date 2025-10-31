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
        private bool _isDisposed;
        TimeSpan currentDuration;

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
            State = TimerState.Idle;
            Color = Configuration.IdleColor;
            currentDuration = Configuration.HeatingDuration;
            UpdateElapsedText(currentDuration);
        }

        // Méthode appelée par le TimerManager
        public void OnTick(DateTime currentTime)
        {
            if (State == TimerState.Idle ) return;

            var elapsed = currentTime - _startTime;
            
            if(elapsed > currentDuration && currentDuration > TimeSpan.Zero)
            {
                HandleEvent(TimerEvent.TickElapsed);
            }

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateElapsedText(elapsed);
            });
        }


        // Gestion des états

        private void HandleEvent(TimerEvent evt)
        {
            var next = GetNextState(State, evt);
            if (next != State)
            {
                MoveToNextState(next);
            }
        }

        private TimerState GetNextState(TimerState state, TimerEvent timerEvent)
        {
            return (state, timerEvent) switch
            {
                // --- IDLE ---
                (TimerState.Idle, TimerEvent.Toggle)
                    => TimerState.Heating,

                // --- HEATING ---
                (TimerState.Heating, TimerEvent.Toggle)
                    => TimerState.Idle,

                (TimerState.Heating, TimerEvent.TickElapsed) when Configuration.CoolingDuration > TimeSpan.Zero
                    => TimerState.WaitingForCooling,

                (TimerState.Heating, TimerEvent.TickElapsed)
                    => TimerState.HeatingDone,

                // --- HEATING DONE ---
                (TimerState.HeatingDone, TimerEvent.Toggle)
                    => TimerState.Idle,

                (TimerState.HeatingDone, TimerEvent.TickElapsed)
                    => TimerState.HeatTimeExceeded,

                // --- HEAT TIME EXCEEDED ---
                (TimerState.HeatTimeExceeded, TimerEvent.Toggle)
                    => TimerState.Idle,

                // --- WAITING FOR COOLING ---
                (TimerState.WaitingForCooling, TimerEvent.Toggle)
                    => TimerState.Cooling,

                (TimerState.WaitingForCooling, TimerEvent.TickElapsed)
                    => TimerState.HeatTimeExceeded,

                // --- COOLING ---
                (TimerState.Cooling, TimerEvent.Toggle)
                    => TimerState.Idle,

                (TimerState.Cooling, TimerEvent.TickElapsed)
                    => TimerState.CoolingDone,

                // --- COOLING DONE ---
                (TimerState.CoolingDone, TimerEvent.Toggle)
                    => TimerState.Idle,

                (TimerState.CoolingDone, TimerEvent.TickElapsed)
                    => TimerState.CoolingTimeExceeded,

                // --- COOLING TIME EXCEEDED ---
                (TimerState.CoolingTimeExceeded, TimerEvent.Toggle)
                    => TimerState.Idle,

                // Default = stay in same state
                _ => state
            };
        }

        private void MoveToNextState(TimerState nextState)
        {
            
            switch (nextState)
            {
                case TimerState.Idle:
                    Color = Configuration.IdleColor;
                    currentDuration = Configuration.HeatingDuration;
                    break;

                case TimerState.Heating:
                    Color = Configuration.HeatingColor;
                    currentDuration = Configuration.HeatingDuration;
                    break;

                case TimerState.HeatingDone:
                    Color = Configuration.HeatingDoneColor;
                    currentDuration = Configuration.MaxHeatTime;
                    break;

                case TimerState.WaitingForCooling:
                    Color = Configuration.WaitingForCoolingColor;
                    currentDuration = Configuration.MaxHeatTime;
                    break;

                case TimerState.Cooling:
                    Color = Configuration.CoolingColor;
                    currentDuration = Configuration.CoolingDuration;
                    break;

                case TimerState.CoolingDone:
                    Color = Configuration.CoolingDoneColor;
                    currentDuration = Configuration.MaxCoolingTime;
                    break;

                case TimerState.HeatTimeExceeded:
                    Color = Configuration.HeatExceededColor;
                    break;

                case TimerState.CoolingTimeExceeded:
                    Color = Configuration.HeatExceededColor;
                    break;
            }

            State = nextState;

            // Reset timer for the new state
            _startTime = DateTime.Now;

            // Mise à jour du texte
            Label = State.ToString();
            UpdateElapsedText(currentDuration);

            System.Diagnostics.Debug.WriteLine(State);
            if (ShouldHaveTimer(State))
                TimerManager.Instance.Register(this);
            else
                TimerManager.Instance.Unregister(this);
        }
        // Méthodes privées
        private void OnToggle() => HandleEvent(TimerEvent.Toggle);
        bool ShouldHaveTimer(TimerState s) => s != TimerState.Idle;

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
            TimerManager.Instance.Unregister(this);
            StateChanged = null;
            TransferRequested = null;
        }
    }

    public class TransferEventArgs : EventArgs
    {
        public TimerCell? Source { get; set; }
        public TimerCell? Destination { get; set; }
    }



    public enum TimerEvent
    {
        Toggle,
        TickElapsed
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
