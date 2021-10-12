using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BalanceAval.Models;
using BalanceAval.Service;
using CsvHelper.Configuration;

namespace BalanceAval.ViewModels
{
    public interface IMainWindowViewModel
    {
        public ObservableCollection<MeasurementSlotVM> Slots { get; set; }


        public ObservableCollection<ICartesianViewModel> CartesianViewModels { get; set; }




        public ICommand Start { get; }


        public ICommand Stop { get; }


        public ICommand Browse { get; }

        public string Folder { get; }

        public bool StartEnabled
        {
            get;
            set;
        }

        public bool StopEnabled { get; set; }

    }


}