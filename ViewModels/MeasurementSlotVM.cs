using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using BalanceAval.Models;
using CsvHelper;

namespace BalanceAval.ViewModels
{
    public class MeasurementSlotVM
    {
        private readonly MeasurementSlot _slot;
        private readonly string _fileName;

        public MeasurementSlotVM(MeasurementSlot slot)
        {
            _slot = slot;
            _fileName = _slot.Time.ToString("yyyy-dd-M--HH-mm-ss") + ".csv";
        }

        public ICommand Save => new Command(WriteToFile);

        private IEnumerable<CSVFormat> GetData()
        {
            using var dbContext = new MyDbContext();
            return dbContext.MeasurementSlots.First(i => i.Id == _slot.Id).MeasurementRows.Select(mr => new CSVFormat()
            {
                X1 = mr.X1,
                X2 = mr.X2,
                X3 = mr.X3,
                X4 = mr.X4,
            });
        }

        public async void WriteToFile(object window)
        {
            using (var writer = new StreamWriter(Path.Combine(Program.UserPath, _fileName)))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(GetData());
            }
        }

        public string Time => _fileName;

        class CSVFormat
        {
            public double X1 { get; set; }
            public double X2 { get; set; }
            public double X3 { get; set; }
            public double X4 { get; set; }
        }
        
    }
}