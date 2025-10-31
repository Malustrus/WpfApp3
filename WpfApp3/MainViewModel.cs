using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp3
{
    public partial class MainViewModel : ObservableObject
    {
        public TimerMatrixViewModel SourceMatrix { get; }
        public TimerMatrixViewModel DestinationMatrix { get; }

        [ObservableProperty] private bool isScanning;
        [ObservableProperty] private ScanPieceViewModel scanViewModel;

        public MainViewModel()
        {
            // Configuration globale
            var timerConfig = new TimerConfiguration
            {
                HeatingDuration = TimeSpan.FromSeconds(5),
                CoolingDuration = TimeSpan.FromSeconds(8),
                MaxCoolingTime = TimeSpan.FromSeconds(10),
                MaxHeatTime = TimeSpan.FromSeconds(7),
                ScanRequired = false,
                FifoMode = false,
                AutoTransfer = false
            };

            SourceMatrix = new TimerMatrixViewModel(20, 10, timerConfig);
            //DestinationMatrix = new TimerMatrixViewModel(3, 3, timerConfig);
            foreach (var cell in SourceMatrix.Cells)
            {
                //cell.ScanRequested += OnScanRequested;
            }

            ScanViewModel = new ScanPieceViewModel();
            ScanViewModel.ScanCompleted += OnScanCompleted;
        }

        private void OnScanRequested(object sender, EventArgs e)
        {
            IsScanning = true;
        }

        private async void OnScanCompleted(bool success)
        {
            IsScanning = false;
            if (success)
            {
                // Lance la minuterie de la cellule scannée
                var cell = SourceMatrix.Cells.FirstOrDefault(c => c.State == TimerState.Idle && c.Configuration.ScanRequired);
                if (cell != null)
                {
                    //cell.StartHeating();
                }
            }
        }
    }
}
