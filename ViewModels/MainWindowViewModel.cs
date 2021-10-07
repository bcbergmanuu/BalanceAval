using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
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
using Stateless;
using Stateless.Graph;

namespace BalanceAval.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IMainWindowViewModel
    {
        private readonly IReadNidaq _nidaq;
        public MainWindowViewModel(IReadNidaq nidaq)
        {
            _nidaq = nidaq;
            CartesianViewModels = new ObservableCollection<ICartesianViewModel>();
            Slots = new ObservableCollection<MeasurementSlot>();

            foreach (var channelName in ReadNidaq.ChannelNames)
            {
                CartesianViewModels.Add(new CartesianViewModel(channelName, ReadNidaq.ChannelNames.IndexOf(channelName)));
            }
            //=> Nidaq_DataReceived
            nidaq.DataReceived += NidaqOnDataReceived;

            FillSlots();

        }

        private void InitializeStateMachine()
        {
            //var phoneCall = new StateMachine<State, Trigger>(State.OffHook);

            //phoneCall.Configure(State.OffHook)
            //    .Permit(Trigger.CallDialled, State.Ringing);

            //phoneCall.Configure(State.Connected)
            //    .OnEntry(t => StartCallTimer())
            //    .OnExit(t => StopCallTimer())
            //    .InternalTransition(Trigger.MuteMicrophone, t => OnMute())
            //    .InternalTransition(Trigger.UnmuteMicrophone, t => OnUnmute())
            //    .InternalTransition<int>(_setVolumeTrigger, (volume, t) => OnSetVolume(volume))
            //    .Permit(Trigger.LeftMessage, State.OffHook)
            //    .Permit(Trigger.PlacedOnHold, State.OnHold);
        }

        private void NidaqOnDataReceived(object? sender, List<AnalogChannel> e)
        {
            Dispatcher.UIThread.InvokeAsync(() => Nidaq_DataReceived(e));
        }

        private void Nidaq_DataReceived(List<Models.AnalogChannel> e)
        {
            UpdateCartesians(e);
            var rows = ToDataRows(e);
            StoreDatabase(rows);
        }

        private void UpdateCartesians(List<AnalogChannel> e)
        {
            foreach (var cartesianViewModel in CartesianViewModels)
            {
                cartesianViewModel.Update(e[cartesianViewModel.Id].Values);
            }
        }



        private static List<MeasurementRow> ToDataRows(List<AnalogChannel> data)
        {
            var contents = new List<List<double>>();
            var rows = new List<MeasurementRow>();
            foreach (var channel in data)
            {
                var lst = new List<double>();
                foreach (var value in channel.Values)
                {
                    lst.Add(value);
                }
                contents.Add(lst);
            }

            for (var i = 0; i < contents[0].Count; i++)
            {
                rows.Add(new MeasurementRow()
                {
                    X1 = contents[0][i],
                    X2 = contents[1][1],
                    X3 = contents[2][i],
                    X4 = contents[3][i],
                });
            }

            return rows;
        }

        private void StoreDatabase(IList<MeasurementRow> data)
        {
            using var dbContext = new MyDbContext();
            //Ensure database is created
            dbContext.Database.EnsureCreated();
            dbContext.MeasurementSlots.Add(new MeasurementSlot());
            dbContext.SaveChanges();
            var lastentry = dbContext.MeasurementSlots.OrderByDescending(s => s.Time).Last();
            Slots.Insert(0,lastentry);

            foreach (var row in data)
            {
                row.Measurement = lastentry.Id;
            }

            dbContext.MeasurementRows.AddRange(data);
            dbContext.SaveChanges();
        }

        private void FillSlots()
        {
            using var dbContext = new MyDbContext();
            Slots.AddRange(dbContext.MeasurementSlots.OrderByDescending(i => i.Id));
        }

        public ObservableCollection<MeasurementSlot> Slots { get; set; }

        public ICommand Save => new Command(WriteToFile);


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
                    using var dbContext = new MyDbContext();
                    csv.WriteRecords(dbContext.MeasurementRows);
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
