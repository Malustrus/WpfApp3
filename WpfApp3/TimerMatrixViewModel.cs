using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp3
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    public partial class TimerMatrixViewModel : ObservableObject
    {
        public ObservableCollection<TimerCell> Cells { get; set; }
        public int Rows { get; }
        public int Columns { get; }

        public TimerMatrixViewModel DestinationMatrix { get; set; }
        public TimerConfiguration Configuration { get; }

        private TimerCell _selectedForTransfer;

        public TimerMatrixViewModel(int rows, int cols, TimerConfiguration config)
        {
            Rows = rows;
            Columns = cols;
            Configuration = config;
            Cells = new ObservableCollection<TimerCell>();

            for (int i = 0; i < rows * cols; i++)
            {
                var cell = new TimerCell(config) { Label = GetLabel(i) };
                cell.StateChanged += (_, __) => UpdateFifoClickable();
                /*cell.ScanRequested += async (_, __) =>
                {
                    if (Configuration.AutoTransfer && DestinationMatrix != null)
                    {
                        await HandleAutomaticTransfer(cell);
                    }
                };*/
                Cells.Add(cell);
            }

            UpdateFifoClickable();
        }

        private string GetLabel(int index)
        {
            int row = index / Columns;
            int col = index % Columns;
            return ExcelNameHelper.GetColumnName(col) + (row + 1);
        }

        public void UpdateFifoClickable()
        {
            if (!Configuration.FifoMode)
            {
                foreach (var c in Cells) c.IsClickable = true;
                return;
            }

            var firstIdle = Cells
                .Where(c => c.State == TimerState.Idle)
                .OrderBy(c => c.LastReadyTime)
                .FirstOrDefault();

            foreach (var c in Cells)
                c.IsClickable = c == firstIdle;
        }

        /*private async Task HandleAutomaticTransfer(TimerCell sourceCell)
        {
            sourceCell.StartHeating();

            // Cherche la première cellule libre dans la destination
            var target = DestinationMatrix.Cells
                .Where(c => c.State == TimerState.Idle)
                .OrderBy(c => c.LastReadyTime)
                .FirstOrDefault();

            if (target != null)
            {
                DestinationMatrix.PerformTransfer(sourceCell, target);
            }
        }

        public void PerformTransfer(TimerCell source, TimerCell destination)
        {
            if (destination.State != TimerState.Idle) return;

            var copy = source.Copy();
            destination.Reset();
            destination.Color = Configuration.IdleColor;
        }*/
    }

}
