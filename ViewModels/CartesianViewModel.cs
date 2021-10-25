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
using SkiaSharp;

namespace BalanceAval.ViewModels
{
    public class CartesianViewModel : ICartesianViewModel
    {
        private int _index = 0;
        private readonly ObservableCollection<ObservablePoint> _observableValues;

        public CartesianViewModel(Channel channel)
        {
            Channel = channel;

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
                }
            };
            YAxes = new List<Axis>
            {
                new Axis // the "units" and "tens" series will be scaled on this axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(25, 118, 210)),
                    Labeler = d => (d * ReadNidaq.MultiplicationFactor).ToString("N") + "kg",
                    TextSize = 10,
                    NameTextSize = 10,
                }
            };

            XAxes = new List<Axis>
            {
                new Axis // the "units" and "tens" series will be scaled on this axis
                {
                    Name = "Channel " + channel.Name,
                    LabelsPaint = new SolidColorPaint(new SKColor(25, 70, 110)),
                    TextSize = 10,
                    NameTextSize = 10,
                    Labeler = d => (d * ReadNidaq.Buffersize).ToString("N")
                }
            };
        }

     

  
        public ObservableCollection<ISeries> Series { get; set; }

        public DrawMarginFrame DrawMarginFrame => new DrawMarginFrame
        {
            Fill = new SolidColorPaint(new SKColor(220, 220, 220)),
            Stroke = new SolidColorPaint(new SKColor(180, 180, 180), 2)
        };

        public IEnumerable<ICartesianAxis> XAxes { get; }
        public IEnumerable<ICartesianAxis> YAxes { get; }

        private void RemoveLastSeries()
        {
            if (_observableValues.Count < 20) return;

            _observableValues.RemoveAt(0);
        }

        public void Update(IEnumerable<double> data)
        {
            _observableValues.Add(new ObservablePoint { X = _index++, Y = data.First() });
            RemoveLastSeries();
        }

        public Channel Channel { get; }
    }
}