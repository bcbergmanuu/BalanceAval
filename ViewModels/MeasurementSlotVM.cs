using System;
using ReactiveUI;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AutoMapper;
using BalanceAval.Models;
using CsvHelper;
using Microsoft.EntityFrameworkCore;

namespace BalanceAval.ViewModels
{
    class MapperConfig
    {
        static MapperConfig()
        {
            var configuration = new MapperConfiguration(cfg => { cfg.CreateMap<CSVFormat, MeasurementRow>(); });
#if DEBUG
            configuration.AssertConfigurationIsValid();
#endif
            Map = new Mapper(configuration);
        }
        public static Mapper Map { get; }
    }

    public class MeasurementSlotVM : ViewModelBase
    {
        private readonly MeasurementSlot _slot;
        private readonly string _fileName;
        private bool _isBusy;
        private string _content;

        public MeasurementSlotVM(MeasurementSlot slot)
        {
            _slot = slot;
            _fileName = _slot.Time.ToString("yyyy-dd-M--HH-mm-ss") + ".csv";
            SetContent();
        }

        private void SetContent()
        {
            if (File.Exists(SavedFileName))
            {
                IsBusy = true;
                Content = "Saved!";
            }

            else
                Content = "Save";
        }

        public ICommand Save => new Command(WriteToFile);



        private IEnumerable<CSVFormat> GetData()
        {
            using var dbContext = new MyDbContext();
            var data = dbContext.MeasurementSlots.Include(s => s.MeasurementRows).First(i => i.Id == _slot.Id).MeasurementRows;

            yield return MapperConfig.Map.Map<CSVFormat>(data);
        }

        public string SavedFileName => Path.Combine(Program.UserPath, _fileName);

        public async void WriteToFile(object window)
        {
            try
            {
                await using var writer = new StreamWriter(SavedFileName);
                await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                await csv.WriteRecordsAsync(GetData());
                SetContent();
            }
            catch (Exception e)
            {
                MainWindowViewModel.Errors.Add(new ErrorModel() { Message = e.Message });
            }
        }


        public string Time => _fileName;

        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        public string Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }
    }
}