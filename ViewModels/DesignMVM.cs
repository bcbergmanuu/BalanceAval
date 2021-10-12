using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BalanceAval.Models;
using BalanceAval.Service;
using NationalInstruments.Restricted;

namespace BalanceAval.ViewModels
{
    public class DesignMVM : ViewModelBase, IMainWindowViewModel
    {
        public DesignMVM()
        {
            

            CartesianViewModels = new ObservableCollection<ICartesianViewModel>();
            Slots = new ObservableCollection<MeasurementSlotVM>();

            foreach (var channelName in ReadNidaq.ChannelNames)
            {
                CartesianViewModels.Add(
                    new CartesianViewModel(channelName, ReadNidaq.ChannelNames.IndexOf(channelName)));
            }

          

            for (int i = 0; i < 10; i++)
            {
                Slots.Add(new MeasurementSlotVM(new MeasurementSlot() {Id = i, Time = DateTime.Now.AddSeconds(i)}));
            }
        }
        //=> Nidaq_DataReceived

        public ObservableCollection<MeasurementSlotVM> Slots { get; set; }


        public ObservableCollection<ICartesianViewModel> CartesianViewModels { get; set; }


        

        public ICommand Start { get; }
    
    
        public ICommand Stop { get; }


        public ICommand Browse { get; }

        public string Folder => Program.UserPath;
        public bool StartEnabled { get; set; }
        public bool StopEnabled { get; set; }
    }
}