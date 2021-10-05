using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Avalonia.Controls;
using BalanceAval.Models;
using BalanceAval.Service;
using CsvHelper;
using CsvHelper.Configuration;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using NationalInstruments.Restricted;
using SkiaSharp;

namespace BalanceAval.ViewModels
{
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


          
        }

        private void Nidaq_DataReceived(object? sender, List<Models.AnalogChannel> e)
        {
            foreach (var cartesianViewModel in CartesianViewModels)
            {
                cartesianViewModel.Update(e[cartesianViewModel.Id].Values);
            }

            Update(e);

            //litesql to store temporary data!
        }

        public ICommand Save => new Command(WriteToFile);

        //private List<CsvRow> ToData(List<Models.AnalogChannel> data)
        //{

        //}

        public async void WriteToFile(object window)
        {
            var dlg = new SaveFileDialog();
            dlg.Filters.Add(new FileDialogFilter() { Name = "csv", Extensions = { "csv" } });

            string? result = await dlg.ShowAsync((Window)window);

            if (result != null)
            {
                using (var writer = new StreamWriter(result))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(new List<CsvRow>());
                }
            }
        }

        public ObservableCollection<ICartesianViewModel> CartesianViewModels { get; set; }

        public ICommand Start => new Command(o => _nidaq.Start());
        public ICommand Stop => new Command(o => _nidaq.Stop());


        public void Update(List<Models.AnalogChannel> data)
        {
            var dat = data.ToArray();

            var fx12 = data[0].Values[0] + data[1].Values[0];
            var fx34 = data[2].Values[0] + data[3].Values[0];
            var fx14 = data[0].Values[0] + data[3].Values[0];
            var fx23 = data[1].Values[0] + data[2].Values[0];
            var fx = fx12 + fx34;
            var fy = fx14 + fx23;
        }



      
        
    }
}
