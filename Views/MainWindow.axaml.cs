using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using BalanceAval.ViewModels;
using JetBrains.Annotations;
using ReactiveUI;

namespace BalanceAval.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }


    public class PolyLineCustom : Polyline
    {
        public static readonly DirectProperty<PolyLineCustom, Point> NewPointProperty =
            AvaloniaProperty.RegisterDirect<PolyLineCustom, Point>(nameof(NewPoint), o => o.NewPoint, (o, v) => o.NewPoint = v);

        private Point _point = new Point();

        public PolyLineCustom()
        {
            NewPointProperty.Changed.AddClassHandler<PolyLineCustom>((x, e) => x.ChangeLines(e));
        }

        protected void ChangeLines(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                _point = (Point)e.NewValue;
                Points.Add(_point);
            }
        }

        private void _items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            throw new System.NotImplementedException("Het werkt!");
        }

        public Point NewPoint
        {
            get { return _point; }
            set { SetAndRaise(NewPointProperty, ref _point, value); }
        }
    }
}
