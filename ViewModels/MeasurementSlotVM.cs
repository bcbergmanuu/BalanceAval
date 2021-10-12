using System;
using ReactiveUI;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using BalanceAval.Models;
using BalanceAval.Service;
using CsvHelper;
using Microsoft.EntityFrameworkCore;

namespace BalanceAval.ViewModels
{
    public class MeasurementSlotVM : ViewModelBase  
    {
        private readonly MeasurementSlot _slot;
        private readonly string _fileName;
        private bool _isBusy;

        public MeasurementSlotVM(MeasurementSlot slot)
        {
            _slot = slot;
            _fileName = _slot.Time.ToString("yyyy-dd-M--HH-mm-ss") + ".csv";
        }

        public ICommand Save => new Command(WriteToFile);

        private IEnumerable<CSVFormat> GetData()
        {
            using var dbContext = new MyDbContext();
            return dbContext.MeasurementSlots.Include(s => s.MeasurementRows).First(i => i.Id == _slot.Id).MeasurementRows.Select(mr => new CSVFormat()
            {
                X1 = (mr.X1 * ReadNidaq.MultiplicationFactor).ToString("N"),
                X2 = (mr.X2 * ReadNidaq.MultiplicationFactor).ToString("N"),
                X3 = (mr.X3 * ReadNidaq.MultiplicationFactor).ToString("N"),
                X4 = (mr.X4 * ReadNidaq.MultiplicationFactor).ToString("N"),
            });
        }

        public async void WriteToFile(object window)
        {
            try
            {
                await using var writer = new StreamWriter(Path.Combine(Program.UserPath, _fileName));
                await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                await csv.WriteRecordsAsync(GetData());
            }
            catch(Exception e)
            {
                MainWindowViewModel.Errors.Add(new ErrorModel(){Message = e.Message});
            }
        }


        public string Time => _fileName;

        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        class CSVFormat
        {
            public string X1 { get; set; }
            public string X2 { get; set; }
            public string X3 { get; set; }
            public string X4 { get; set; }
        }

  
    }
}