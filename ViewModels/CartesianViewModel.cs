using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BalanceAval.Service;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;
using SkiaSharp;

namespace BalanceAval.ViewModels
{
    public class CartesianViewModel : ViewModelBase, ICartesianViewModel
    {
        private int _index = 0;
        private readonly ObservableCollection<ObservablePoint> _observableValues;
        private string _lastItem;

        private Dictionary<string, string> _sensornameLookup = new()
        {
            {"Z1", "Fz Sensor 1 (front left"},
            {"Z2", "Fz Sensor 2 (front right)"},
            { "Z3", "Fz Sensor 3 (back left)" },
            { "Z4", "Fz Sensor 4 (back right)" },
            { "X1", "Fx Sensor 1,3 (anterior-posterior, left)" },
            { "X2", "Fx Sensor 2,4 (anterior-posterior, right)" },
            { "Y", "Fy (mediolateral)" },
        };

        public CartesianViewModel(string channel)
        {
            ChannelName = channel;

            _observableValues = new ObservableCollection<ObservablePoint>();
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservablePoint>
                {
                    Values = _observableValues,
                    Fill = null,
                    GeometryStroke = null,
                    GeometryFill = null,
                    GeometrySize = 0,
                    DataPadding = new LvcPoint(0,0),
                    DataLabelsSize = 10,
                    ScalesXAt = 0,

                },
            };
            YAxes = new List<Axis>
            {
                new Axis // the "units" and "tens" series will be scaled on this axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(25, 118, 210)),
                    Labeler = d => (d * ReadNidaq.MultiplicationFactor).ToString("N") + "kg",
                    TextSize = 10,
                    NameTextSize = 10,
                    MinLimit = -.2,
                    MaxLimit = 4
                }
            };

            XAxes = new List<Axis>
            {
                new Axis // the "units" and "tens" series will be scaled on this axis
                {
                    Name = _sensornameLookup[channel],
                    LabelsPaint = new SolidColorPaint(new SKColor(25, 70, 110)),
                    TextSize = 10,
                    NameTextSize = 10,
                    Labeler = d => (d * ReadNidaq.Buffersize).ToString("N")
                }
            };
        }

     

  
        public ObservableCollection<ISeries> Series { get; set; }

        public void ResetData()
        {
            _observableValues.Clear();
        }

        public DrawMarginFrame DrawMarginFrame => new DrawMarginFrame
        {
            Fill = new SolidColorPaint(new SKColor(220, 220, 220)),
            Stroke = new SolidColorPaint(new SKColor(180, 180, 180), 2)
        };

        public IEnumerable<ICartesianAxis> XAxes { get; }
        public IEnumerable<ICartesianAxis> YAxes { get; }

        public string SensorName => _sensornameLookup[ChannelName];

        private void RemoveLastSeries()
        {
            if (_observableValues.Count < 20) return;

            _observableValues.RemoveAt(0);
        }

        public string LastItem
        {
            get => _lastItem;
            set => this.RaiseAndSetIfChanged(ref _lastItem, value);
        }

        public void Update(IEnumerable<double> data)
        {
            var yValue = data.Last();
            _observableValues.Add(new ObservablePoint { X = _index++, Y = yValue });
            LastItem = (yValue * ReadNidaq.MultiplicationFactor).ToString("N");
            RemoveLastSeries();
        }

        public string ChannelName { get; }
    }
}