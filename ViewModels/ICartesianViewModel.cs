using System.Collections.Generic;
using System.Collections.ObjectModel;
using BalanceAval.Service;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;

namespace BalanceAval.ViewModels
{
    public interface ICartesianViewModel
    {
        public ObservableCollection<ISeries> Series { get; set; }

        public void Update(IEnumerable<double> data);
        public string ChannelName { get; }


        public DrawMarginFrame DrawMarginFrame { get; }
        public IEnumerable<ICartesianAxis> XAxes { get; }
        public IEnumerable<ICartesianAxis> YAxes { get; }

    }
}