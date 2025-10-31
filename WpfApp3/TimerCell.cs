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
            State = TimerState.Idle;
            oldState = TimerState.Idle;
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
                timeElapsed = true;
                MoveToNextState();
            }

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateElapsedText(elapsed);
            });
        }

        bool toggleRequest;
        bool timeElapsed;
        TimeSpan currentDuration;
        // Gestion des états
        private void MoveToNextState()
        {
            switch (State)
            {
                case TimerState.Idle:
                    State = TimerState.Heating;
                    break;
                case TimerState.Heating when toggleRequest:
                    State = TimerState.Idle;
                    break;
                case TimerState.Heating when timeElapsed && Configuration.CoolingDuration > TimeSpan.Zero:
                    State = TimerState.WaitingForCooling;
                    break;
                case TimerState.Heating when timeElapsed && Configuration.CoolingDuration <= TimeSpan.Zero:
                    State = TimerState.HeatingDone;
                    break;
                case TimerState.HeatingDone when toggleRequest && !timeElapsed:
                    State = TimerState.Idle;
                    break;
                case TimerState.HeatingDone when !toggleRequest && timeElapsed:
                    State = TimerState.HeatTimeExceeded;
                    break;
                case TimerState.HeatTimeExceeded when toggleRequest:
                    State = TimerState.Idle;
                    break;
                case TimerState.WaitingForCooling when !toggleRequest && timeElapsed:
                    State = TimerState.HeatTimeExceeded;
                    break;
                case TimerState.WaitingForCooling when toggleRequest && !timeElapsed:
                    State = TimerState.Cooling;
                    break;
                case TimerState.Cooling when toggleRequest && !timeElapsed:
                    State = TimerState.Idle;
                    break;
                case TimerState.Cooling when !toggleRequest && timeElapsed:
                    State = TimerState.CoolingDone;
                    break;
                case TimerState.CoolingDone when toggleRequest && !timeElapsed:
                    State = TimerState.Idle;
                    break;
                case TimerState.CoolingDone when !toggleRequest && timeElapsed:
                    State = TimerState.CoolingTimeExceeded;
                    break;
                case TimerState.CoolingTimeExceeded when toggleRequest:
                    State = TimerState.Idle;
                    break;
            }

            if(State != oldState)
            {
                oldState = State;
                switch (State)
                {
                    case TimerState.Idle:
                        TimerManager.Instance.Unregister(this);                       
                        Color = Configuration.IdleColor;                        
                        LastReadyTime = DateTime.Now;
                        break;
                    case TimerState.Heating:                        
                        TimerManager.Instance.Register(this);
                        Color = Configuration.HeatingColor;
                        currentDuration = Configuration.HeatingDuration;
                        break;
                    case TimerState.HeatingDone:
                        Color = Configuration.HeatingDoneColor;
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
                    case TimerState.WaitingForCooling:
                        Color = Configuration.WaitingForCoolingColor;
                        currentDuration = Configuration.MaxHeatTime;
                        break;
                    case TimerState.HeatTimeExceeded:
                        Color = Configuration.HeatExceededColor;
                        break;
                    case TimerState.CoolingTimeExceeded:
                        Color = Configuration.HeatExceededColor;
                        break;
                }
                _startTime = DateTime.Now;
                Label = State.ToString();
                UpdateElapsedText(currentDuration);
                System.Diagnostics.Debug.WriteLine(State);
            }
            toggleRequest = false;
            timeElapsed = false;
        }


        // Méthodes privées
        private void OnToggle()
        {
            toggleRequest = true;  
            MoveToNextState();
            
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
