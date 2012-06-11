using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;

namespace BodyOrientationControlLib
{
    public class LivePlotter : Canvas
    {
        private delegate void Redraw(Func<double, string> label, Func<double> valueToPlot);

        private Thread _thread = null;
        private volatile bool _aborted = true;
        
        private static Color WhiteColor = Color.FromArgb(255, 255, 255, 255);
        private static Brush WhiteBrush = new SolidColorBrush(WhiteColor);

        private GeometryGroup _geometry = null;
        private Path _path = null;
        private double _lastReading = double.NaN;
        private int _counter = 0;
        private TextBlock _label = null;

        private double _min = 0;
        private double _max = 0;

        public LivePlotter()
        {
            // Initialize the path and its geometry. It will be filled 
            // with the data to be plotted
            _geometry = new GeometryGroup();
            _path = new Path()
            {
                Data = _geometry,
                Stroke = WhiteBrush,
                StrokeThickness = 1
            };
            Children.Add(_path);
            this.InvalidateVisual();

            // Init the textblock label that displays the current value as text
            _label = new TextBlock(){ 
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(6, 2, 0, 0),
                Foreground = WhiteBrush
            };
            Children.Add(_label);
        }

        public void Start(Func<double, string> label, Func<double> valueToPlot, double def, double min, double max)
        {
            _min = min;
            _max = max;
            _aborted = false;
            _geometry.Children.Clear();
            _counter = 0;
            for (int i = 0; i < ActualWidth; i++)
                PlotSingleValue(def);

            if (_thread == null)
            {
                _min = min;
                _max = max;
                _aborted = false;
                _thread = new Thread(new ThreadStart(() =>
                    {
                        while (!_aborted)
                        {
                            // Call the update function every n milliseconds
                            Dispatcher.BeginInvoke(new Redraw(ValueChanged), label, valueToPlot);
                            Thread.Sleep(50);
                        }
                    }));
                _thread.Start();
            }
        }

        public void Stop()
        {
            if (_thread != null && _thread.IsAlive && !_aborted)
                _aborted = true;
        }

        private void ValueChanged(Func<double, string> label, Func<double> valueToPlot)
        {
            double val = valueToPlot();

            // Show the value in the label
            _label.Text = label(val);

            // Plot the value
            PlotSingleValue(val);
        }

        private void PlotSingleValue(double val)
        {
            // Normalize the value to the vertical bounds
            var curReading = (-((val - _min) / (_max - _min) - 0.5) + 0.5) * (ActualHeight);

            // Initialize the first reading
            if (_lastReading == double.NaN)
                _lastReading = curReading;

            // Add new path segment to the right
            _geometry.Children.Add(new LineGeometry()
            {
                StartPoint = new System.Windows.Point(ActualWidth + _counter - 1, _lastReading),
                EndPoint = new System.Windows.Point(ActualWidth + _counter, curReading)
            });

            // Move everything to the left (by changing the margin of the path)
            // And remove the last path section that falls out of the left bound
            _path.Margin = new Thickness(-_counter, 0, 0, 0);
            if (_counter > ActualWidth)
                _geometry.Children.RemoveAt(0);

            // Store these values. We need them next time we get called
            _counter++;
            _lastReading = curReading;
        }
    }
}
