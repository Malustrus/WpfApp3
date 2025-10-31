using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfApp3
{
    public class TimerConfiguration
    {
        // Durée et refroidissement
        public TimeSpan HeatingDuration { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan CoolingDuration { get; set; } = TimeSpan.FromSeconds(5);

        // Temps max optionnels
        public TimeSpan MaxHeatTime { get; set; } = TimeSpan.Zero;
        public TimeSpan MaxCoolingTime { get; set; } = TimeSpan.Zero;

        public bool ScanRequired { get; set; } = false;
        public bool FifoMode { get; set; } = true;

        // Transfert
        public bool AutoTransfer { get; set; } = false; // si vrai, la destination est choisie automatiquement

        // Couleurs
        public Brush IdleColor { get; set; } = Brushes.LightGray;
        public Brush HeatingColor { get; set; } = Brushes.Red;
        public Brush CoolingColor { get; set; } = Brushes.LightBlue;
        public Brush WaitingForCoolingColor { get; set; } = Brushes.Orange;
        public Brush HeatExceededColor { get; set; } = Brushes.DarkRed;
        public Brush CoolingExceededColor { get; set; } = Brushes.DarkBlue;
        public Brush TransferHighlightColor { get; set; } = Brushes.Yellow;
        public Brush HeatingDoneColor { get; set; } = Brushes.Green;
        public Brush CoolingDoneColor { get; set; } = Brushes.LightGreen;

    }
}
