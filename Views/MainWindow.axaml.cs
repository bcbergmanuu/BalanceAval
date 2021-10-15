using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
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




    public class CanvasCustom : Canvas
    {
        public static readonly DirectProperty<CanvasCustom, Point> NewPointProperty =
            AvaloniaProperty.RegisterDirect<CanvasCustom, Point>(nameof(NewPoint), o => o.NewPoint, (o, v) => o.NewPoint = v);

        public Point NewPoint
        {
            get { return _point; }
            set { SetAndRaise(NewPointProperty, ref _point, value); }
        }

        private Point _point;

        public CanvasCustom()
        {
            NewPointProperty.Changed.AddClassHandler<CanvasCustom>((x, e) => x.ChangeLines(e));
    

        }

        private Point _oldPoint;

        protected void ChangeLines(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue == null) return;

            var newPoint = (Point)e.NewValue;

            var l = new Line
            {
                StartPoint = toCenter(_oldPoint),
                EndPoint = toCenter(newPoint),
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(PickBrush())
            };

            Children.Add(l);


            _oldPoint = newPoint;
        }

        private const double Shift = 150.0;
        private const double Amplify = 1;
        private Point toCenter(Point input)
        {
            return new Point(input.X*Amplify + Shift, input.Y*Amplify + Shift);
        }

        private Color PickBrush()
        {
            Random rnd = new Random();

            Type brushesType = typeof(Brushes);

            PropertyInfo[] properties = brushesType.GetProperties();

            int random = rnd.Next(properties.Length-6);
            var result = (ImmutableSolidColorBrush)properties[random].GetValue(null, null);

            return result.Color;
        }

    }
}
