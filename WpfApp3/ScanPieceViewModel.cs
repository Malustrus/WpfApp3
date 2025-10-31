using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp3
{
    public partial class ScanPieceViewModel : ObservableRecipient
    {
        [ObservableProperty] private string scanValue;

        public IRelayCommand ConfirmScanCommand { get; }
        public IRelayCommand CancelScanCommand { get; }

        public event Action<bool> ScanCompleted;

        public ScanPieceViewModel()
        {
            ConfirmScanCommand = new RelayCommand(() => ScanCompleted?.Invoke(true));
            CancelScanCommand = new RelayCommand(() => ScanCompleted?.Invoke(false));
        }
    }
}
