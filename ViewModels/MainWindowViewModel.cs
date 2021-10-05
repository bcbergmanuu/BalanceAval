using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using BalanceAval.Service;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using NationalInstruments.Restricted;
using SkiaSharp;

namespace BalanceAval.ViewModels
{
    public interface IMainWindowViewModel
    {

    }

    public class MainWindowViewModel : ViewModelBase, IMainWindowViewModel
    {
        private readonly IReadNidaq _nidaq;
        public MainWindowViewModel(IReadNidaq nidaq)
        {
            _nidaq = nidaq;
            CartesianViewModels = new ObservableCollection<ICartesianViewModel>();
            foreach (var channelName in ReadNidaq.ChannelNames)
            {
                CartesianViewModels.Add(new CartesianViewModel(channelName, ReadNidaq.ChannelNames.IndexOf(channelName)));
            }

            nidaq.DataReceived += Nidaq_DataReceived;


            observableValues = new ObservableCollection<WeightedPoint>
            {
                new WeightedPoint(0, 0, 1)
            };

            Series = new ObservableCollection<ISeries>
            {
                new ScatterSeries<WeightedPoint> { Values = observableValues, GeometrySize = 50 }
            };
        }

        private void Nidaq_DataReceived(object? sender, List<Models.AnalogChannel> e)
        {
            foreach (var cartesianViewModel in CartesianViewModels)
            {
                cartesianViewModel.Update(e[cartesianViewModel.Id].Values);
            }

            Update(e);
        }

        public ObservableCollection<ICartesianViewModel> CartesianViewModels { get; set; }

        public ICommand Start => new Command(o => _nidaq.Start());
        public ICommand Stop => new Command(o => _nidaq.Stop());


        private ObservableCollection<WeightedPoint> observableValues;


        public ObservableCollection<ISeries> Series { get; set; }
        public void Update(List<Models.AnalogChannel> data)
        {
            var dat = data.ToArray();

            var fx12 = data[0].Values[0] + data[1].Values[0];
            var fx34 = data[2].Values[0] + data[3].Values[0];
            var fx14 = data[0].Values[0] + data[3].Values[0];
            var fx23 = data[1].Values[0] + data[2].Values[0];
            var fx = fx12 + fx34;
            var fy = fx14 + fx23;

            RemoveFirstItem();
            Add(fx, fy);
        }

        private void Add(double x, double y)
        {
            observableValues.Add(new WeightedPoint(x, y, 1));
        }

        private void RemoveFirstItem()
        {
            if (observableValues.Count < 1) return;
            observableValues.RemoveAt(0);
        }

    }
}
