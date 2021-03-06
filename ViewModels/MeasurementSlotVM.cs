using System;
using ReactiveUI;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MeasurementRow, CSVFormat>();
            });

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
        private bool _isBusy;
        private string _content = "Save";

        public MeasurementSlotVM(MeasurementSlot slot)
        {
            _slot = slot;
            FileName = _slot.Time.ToString("yyyy-dd-MM-HH-mm-ss");
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
            {
                Content = "Save";
                IsBusy = false;
            }
                
        }

        public ICommand Save => new Command(WriteToFile);



        private async IAsyncEnumerable<CSVFormat> GetData()
        {
            await using var dbContext = new MyDbContext();
            var data = dbContext.MeasurementSlots.Include(s => s.MeasurementRows).First(i => i.Id == _slot.Id).MeasurementRows;

            foreach (var measurementRow in data)
            {
                yield return MapperConfig.Map.Map<CSVFormat>(measurementRow);
            }
        }

        private string Extension = ".csv";
        public string SavedFileName => Path.Combine(Program.UserPath, FileName + Extension);

        public async void WriteToFile(object window)
        {
            IsBusy = true;
            try
            {
                await using var writer = new StreamWriter(SavedFileName);
                await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                await csv.WriteRecordsAsync(GetData());

            }
            catch (Exception e)
            {
                MainWindowViewModel.Errors.Add(new ErrorModel() {Message = e.Message});
            }
            finally
            {
                SetContent();
            }
        }


        public string FileName { get; set; }

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