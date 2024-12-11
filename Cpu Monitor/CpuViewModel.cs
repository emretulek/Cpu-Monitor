using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Widgets.Common;

namespace CPU_Monitor
{
    public class CpuViewModel: INotifyPropertyChanged,IDisposable
    {
        private readonly Schedule schedule = new();
        private string scheduleID = "";
        private readonly CancellationTokenSource cancellationTokenSource = new();

        public struct SettingsStruct
        {
            public float TimeLine { get; set; }
            public string GraphicColor { get; set; }
        }

        public static SettingsStruct Default => new()
        {
            TimeLine = 200,
            GraphicColor = "#347aeb",
        };

        public required SettingsStruct Settings = Default;

        private PerformanceCounter? cpuCounter;
        private AreaSeries? AreaSeries;
        private int timeCounter;


        private PlotModel? _plotModel;
        public PlotModel? CpuPlotModel 
        {
            get { return _plotModel; }
            set { 
                _plotModel = value;
                OnPropertyChanged(nameof(CpuPlotModel));
            }
        }

        private string _cpuUsageText = "0";
        public string CpuUsageText
        {
            get { return _cpuUsageText; } 
            set { 
                _cpuUsageText = value;
                OnPropertyChanged(nameof(CpuUsageText));
            }
        }

        public async Task Start()
        {
            await Task.Run(() =>
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }, cancellationTokenSource.Token);

            CreatePlot();
            UpdateCpuUsage();

            scheduleID = schedule.Secondly(UpdateCpuUsage, 1);
        }

        private void CreatePlot()
        {
            CpuPlotModel = new PlotModel
            {
                PlotAreaBorderThickness = new OxyThickness(0),
                PlotAreaBorderColor = OxyColors.Transparent,
                Padding = new OxyThickness(0)
            };

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                IsAxisVisible = false,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                MaximumPadding = 0,
                MinimumPadding = 0                  
            };

            var yAxis = new LinearAxis
            {
                Minimum = 0,
                Maximum = 100,
                Position = AxisPosition.Left,
                IsAxisVisible = false,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                MaximumPadding = 0,
                MinimumPadding = 0
            };

            CpuPlotModel.Axes.Add(xAxis);
            CpuPlotModel.Axes.Add(yAxis);

            AreaSeries = new AreaSeries
            {
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1,
                Color = OxyColor.Parse(Settings.GraphicColor)
            };

            CpuPlotModel.Series.Add(AreaSeries);
        }

        private void UpdateCpuUsage()
        {
            if (CpuPlotModel is null || cpuCounter is null || AreaSeries is null) return;

            double cpuUsage = cpuCounter.NextValue();

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    CpuUsageText = $"CPU {cpuUsage:F2}%";

                    AreaSeries.Points.Add(new DataPoint(timeCounter, cpuUsage));
                    AreaSeries.Points2.Add(new DataPoint(timeCounter, 0));
                    timeCounter++;

                    if (AreaSeries.Points.Count > Settings.TimeLine)
                    {
                        AreaSeries.Points.RemoveAt(0);
                        AreaSeries.Points2.RemoveAt(0);
                    }

                    CpuPlotModel.InvalidatePlot(true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            });
        }

        public void Dispose()
        {
            schedule.Stop(scheduleID);
            cancellationTokenSource.Cancel();
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
