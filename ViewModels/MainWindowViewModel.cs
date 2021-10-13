using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Threading;
using BalanceAval.Models;
using BalanceAval.Service;
using NationalInstruments.Restricted;
using ReactiveUI;
using Stateless;

namespace BalanceAval.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IMainWindowViewModel
    {
        private readonly IReadNidaq _nidaq;
        public MainWindowViewModel(IReadNidaq nidaq)
        {
            Errors = new ObservableCollection<ErrorModel>();
            
            ConfigureStateMachine();
            _nidaq = nidaq;

            CartesianViewModels = new ObservableCollection<ICartesianViewModel>();
            Slots = new ObservableCollection<MeasurementSlotVM>();

            foreach (var channelName in ReadNidaq.ChannelNames)
            {
                CartesianViewModels.Add(new CartesianViewModel(channelName, ReadNidaq.ChannelNames.IndexOf(channelName)));
            }
            nidaq.Error += NidaqOnError;
            nidaq.DataReceived += NidaqOnDataReceived;
            FillSlots();
        }

        private void NidaqOnError(object? sender, string e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _stateMachine.Fire(NidaqTriggers.Error);
                Errors.Insert(0, new ErrorModel() { Message = e });
            });
        }

        public static ObservableCollection<ErrorModel> Errors { get; set; }

        private enum NidaqTriggers
        {
            Start,
            Stop,
            Error,
            DelayFinished,
        }
        public enum NidaqStates
        {
            Running,
            Stopped,
            TimeOut,
        }

        private readonly StateMachine<NidaqStates, NidaqTriggers> _stateMachine = new StateMachine<NidaqStates, NidaqTriggers>(NidaqStates.Stopped);

        private void ConfigureStateMachine()
        {

            _stateMachine.Configure(NidaqStates.Running).OnEntry(() =>
                {
                    Errors.Clear();
                    StartEnabled = false;
                    StopEnabled = true;
                    StartSlot();
                    _nidaq.Start();
                })
                .Permit(NidaqTriggers.Stop, NidaqStates.TimeOut)
                .Permit(NidaqTriggers.Error, NidaqStates.TimeOut);

            _stateMachine.Configure(NidaqStates.Stopped).OnEntry(() =>
                {
                    StartEnabled = true;
                })
                .Permit(NidaqTriggers.Start, NidaqStates.Running);
            _stateMachine.Configure(NidaqStates.TimeOut).OnEntry(() =>
            {
                StopEnabled = false;
                _nidaq.Stop();
                DisplayLastSlot();
                Task.Run(async delegate
                {
                    await Task.Delay(5000);
                    _stateMachine.Fire(NidaqTriggers.DelayFinished);
                });

            }).Permit(NidaqTriggers.DelayFinished, NidaqStates.Stopped);
        }


        private void NidaqOnDataReceived(object? sender, List<AnalogChannel> e)
        {
            Dispatcher.UIThread.InvokeAsync(() => Nidaq_DataReceived(e));
        }

        private void Nidaq_DataReceived(List<AnalogChannel> e)
        {
            UpdateCartesians(e);
            var rows = ToDataRows(e);
            StoreDatabase(rows);
            AddPoints(rows);
        }

        private void UpdateCartesians(IReadOnlyList<AnalogChannel> e)
        {
            foreach (var cartesianViewModel in CartesianViewModels)
            {
                cartesianViewModel.Update(e[cartesianViewModel.Id].Values);
            }
        }

        private bool _startEnabled = true;
        private bool _stopEnabled;
        private Point _newPoint;

        public bool StopEnabled
        {
            get => _stopEnabled;
            set => this.RaiseAndSetIfChanged(ref _stopEnabled, value);
        }

        public bool StartEnabled
        {
            get => _startEnabled;
            set => this.RaiseAndSetIfChanged(ref _startEnabled, value);
        }

        private static IEnumerable<MeasurementRow> ToDataRows(IEnumerable<AnalogChannel> data)
        {
            var rows = new List<MeasurementRow>();
            var contents = data.Select(channel => channel.Values.ToList()).ToList();

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

        private async void DisplayLastSlot()
        {
            await using var dbContext = new MyDbContext();
            //Ensure database is created
            var slot = Slots.First();
            slot.IsBusy = false;
        }

        private async void StartSlot()
        {
            await using var dbContext = new MyDbContext();
            var slot = new MeasurementSlot();
            await dbContext.Database.EnsureCreatedAsync();
            dbContext.MeasurementSlots.Add(slot);
            await dbContext.SaveChangesAsync();
            Slots.Insert(0, new MeasurementSlotVM(slot) { IsBusy = true });
        }

        private static async void StoreDatabase(IEnumerable<MeasurementRow> data)
        {
            await using var dbContext = new MyDbContext();
            var current = dbContext.MeasurementSlots.OrderByDescending(i => i.Id).First();
            var attachedSlot = data.Select(s =>
            {
                s.MeasurementSlot = current;
                return s;
            }).ToList();
            dbContext.MeasurementRows.AddRange(attachedSlot);
            await dbContext.SaveChangesAsync();
        }

        private async void FillSlots()
        {
            await using var dbContext = new MyDbContext();
            await dbContext.Database.EnsureCreatedAsync();
            var slots = dbContext.MeasurementSlots.OrderByDescending(i => i.Id).Select(n => new MeasurementSlotVM(n));
            Slots.AddRange(slots);
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

        public ICommand Browse
        {
            get
            {
                return new Command(((d) =>
                {
                    Process.Start(Program.UserPath);
                }));
            }
        }


        public string Folder => Program.UserPath;

        public Point NewPoint
        {
            get => _newPoint;
            set => this.RaiseAndSetIfChanged(ref _newPoint, value);
        }


        private void AddPoints(IEnumerable<MeasurementRow> data)
        {
            NewPoint = (COP(data.First()));
        }


        public Point COP(MeasurementRow r)
        {
            return new Point((r.X4 + r.X2) - (r.X1 + r.X3) * 20, (r.X3 + r.X4) - (r.X1 + r.X2) * 40);
        }
    }
}
