using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

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

        static MainWindowViewModel()
        {
            Errors = new ObservableCollection<ErrorModel>();
        }
        public MainWindowViewModel(IReadNidaq nidaq)
        {



            ConfigureStateMachine();
            _nidaq = nidaq;

            CartesianViewModels = new ObservableCollection<ICartesianViewModel>();
            Slots = new ObservableCollection<MeasurementSlotVM>();

            foreach (var channelName in ReadNidaq.Channels)
            {
                CartesianViewModels.Add(new CartesianViewModel(channelName.Key));
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

        private readonly StateMachine<NidaqStates, NidaqTriggers> _stateMachine = new(NidaqStates.Stopped);

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
                    _nidaq.Stop();
                })
                .Permit(NidaqTriggers.Start, NidaqStates.Running);
            _stateMachine.Configure(NidaqStates.TimeOut).OnEntry(() =>
            {
                StopEnabled = false;
                _nidaq.Stop();
                DisplayLastSlot();
                Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    await _stateMachine.FireAsync(NidaqTriggers.DelayFinished);
                });

            }).Permit(NidaqTriggers.DelayFinished, NidaqStates.Stopped);
        }


        private void NidaqOnDataReceived(object? sender, List<AnalogChannel> e)
        {
            Dispatcher.UIThread.InvokeAsync(() => Nidaq_DataReceived(e));
        }

        private void Nidaq_DataReceived(IReadOnlyList<AnalogChannel> e)
        {
            UpdateCartesians(e);
            var rows = ToDataRows(e);
            var measurementRows = rows as MeasurementRow[] ?? rows.ToArray();
            StoreDatabase(measurementRows);
        }

        private void UpdateCartesians(IEnumerable<AnalogChannel> e)
        {

            foreach (var analogChannel in e)
            {
                CartesianViewModels.First(q => q.ChannelName.Equals(ReadNidaq.Channels[analogChannel.NiInput])).Update(analogChannel.Values);
            }
        }

        private bool _startEnabled = true;
        private bool _stopEnabled;

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

        

        public static IEnumerable<MeasurementRow> ToDataRows(IReadOnlyList<AnalogChannel> data)
        {
            var orderofChannels = data.Select(s => s.NiInput).ToArray();
            var rows = new List<MeasurementRow>();
            for (var i = 0; i < data.First().Values.Count; i++)
            {
                var instance = new MeasurementRow();

                for (var j = 0; j < orderofChannels.Length; j++)
                {
                    typeof(MeasurementRow).GetProperty(ReadNidaq.Channels[orderofChannels[j]])
                        .SetValue(instance, data[j].Values[i] * ReadNidaq.MultiplicationFactor);
                }

                rows.Add(instance);
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
                return new Command((_ =>
                {
                    try
                    {
                        Process.Start(Program.UserPath);
                    }
                    catch (Exception e)
                    {
                        Errors.Add(new ErrorModel() { Message = e.Message });
                    }
                }));
            }
        }

        public string Folder => Program.UserPath;

    }


}
