using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using BalanceAval.Service;
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
            CartesianViewModels = new ObservableCollection<CartesianViewModel>();
            foreach (var channelName in ReadNidaq.ChannelNames)
            {
                CartesianViewModels.Add(new CartesianViewModel() {Name = channelName, Id = ReadNidaq.ChannelNames.IndexOf(channelName)});
            }

            nidaq.DataReceived += Nidaq_DataReceived;
        }

        private void Nidaq_DataReceived(object? sender, List<Models.AnalogChannel> e)
        {
            foreach (var cartesianViewModel in CartesianViewModels)
            {
                cartesianViewModel.Update(e[cartesianViewModel.Id]);
            }
        }

        public ObservableCollection<CartesianViewModel> CartesianViewModels { get; set; }




        public ICommand Start => new Command(o => _nidaq.Start());
        public ICommand Stop => new Command(o => _nidaq.Stop());
    }
}
