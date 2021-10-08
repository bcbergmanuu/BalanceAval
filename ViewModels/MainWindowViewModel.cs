using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using BalanceAval.Models;
using BalanceAval.Service;
using CsvHelper.Configuration;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using NationalInstruments.Restricted;
using ReactiveUI;
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
            ConfigureStateMachine();
            _nidaq = nidaq;
            CartesianViewModels = new ObservableCollection<ICartesianViewModel>();
            Slots = new ObservableCollection<MeasurementSlotVM>();

            foreach (var channelName in ReadNidaq.ChannelNames)
            {
                CartesianViewModels.Add(new CartesianViewModel(channelName, ReadNidaq.ChannelNames.IndexOf(channelName)));
            }

            //=> Nidaq_DataReceived
            nidaq.DataReceived += NidaqOnDataReceived;

            FillSlots();

        }

        private enum NidaqTriggers
        {
            Start,
            Stop,
            Error,
        }
        public enum NidaqStates
        {
            Running,
            Stopped,
        }

        private readonly StateMachine<NidaqStates, NidaqTriggers> _stateMachine = new StateMachine<NidaqStates, NidaqTriggers>(NidaqStates.Stopped);

        private void ConfigureStateMachine()
        {

            _stateMachine.Configure(NidaqStates.Running).OnEntry(() =>
                {
                    StartEnabled = false;
                    StartSlot();
                    _nidaq.Start();
                })
                .Permit(NidaqTriggers.Stop, NidaqStates.Stopped)
                .Permit(NidaqTriggers.Error, NidaqStates.Stopped);

            _stateMachine.Configure(NidaqStates.Stopped).OnEntry(() =>
                {
                    DisplayLastSlot();
                    StartEnabled = true;
                    _nidaq.Stop();

                })
                .Permit(NidaqTriggers.Start, NidaqStates.Running);
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

        private bool _startEnabled = true;
        public bool StartEnabled
        {
            get => _startEnabled;
            set => this.RaiseAndSetIfChanged(ref _startEnabled, value);
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



        private void DisplayLastSlot()
        {
            using var dbContext = new MyDbContext();
            //Ensure database is created
            var slot = dbContext.MeasurementSlots.OrderByDescending(i => i.Id).First();
            Slots.Insert(0, new MeasurementSlotVM(slot));
        }

        private void StartSlot()
        {
            using var dbContext = new MyDbContext();

            dbContext.Database.EnsureCreated();
            dbContext.MeasurementSlots.Add(new MeasurementSlot());
            dbContext.SaveChanges();
        }

        private void StoreDatabase(IEnumerable<MeasurementRow> data)
        {
            using var dbContext = new MyDbContext();
            var current = dbContext.MeasurementSlots.OrderByDescending(i => i.Id).First();
            var attachedSlot = data.Select(s =>
            {
                s.MeasurementSlot = current;
                return s;
            }).ToList();
            dbContext.MeasurementRows.AddRange(attachedSlot);
            dbContext.SaveChanges();
        }

        private void FillSlots()
        {
            using var dbContext = new MyDbContext();
            dbContext.Database.EnsureCreated();
            Slots.AddRange(dbContext.MeasurementSlots.OrderByDescending(i => i.Id).Select(n => new MeasurementSlotVM(n)));
        }

        public ObservableCollection<MeasurementSlotVM> Slots { get; set; }


        public ObservableCollection<ICartesianViewModel> CartesianViewModels { get; set; }

        private void StartMeasure(object o)
        {
            _stateMachine.Fire(NidaqTriggers.Start);
        }

        private void StopMeasure(object o)
        {
            _stateMachine.Fire(NidaqTriggers.Stop);
        }

        public ICommand Start => new Command(StartMeasure);
        public ICommand Stop => new Command(StopMeasure);

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
