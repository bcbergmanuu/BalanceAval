using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace BalanceAval.ViewModels
{
    public class CartesianViewModel
    {
        private int _index = 0;
        private readonly ObservableCollection<ObservablePoint> _observableValues;

        public CartesianViewModel()
        {

            _observableValues = new ObservableCollection<ObservablePoint>();
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservablePoint>
                {
                    Values = _observableValues,
                    Fill = null
                }
            };
        }

        public string Name { get; set; }

        public int Id { get; set; }

        public ObservableCollection<ISeries> Series { get; set; }

        public void RemoveLastSeries()
        {
            if (Series.Count < 100) return;

            Series.RemoveAt(0);
        }

        public void Update(Models.AnalogChannel data)
        {
            _observableValues.Add(new ObservablePoint { X = _index++, Y = data.Values[0] });
            RemoveLastSeries();
        }
    }
}