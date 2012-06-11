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
using System.Linq;

namespace BodyOrientationControlLib
{
    public class FunctionPlotter : Canvas
    {
        private static Color WhiteColor = Color.FromArgb(255, 255, 255, 255);
        private static Brush WhiteBrush = new SolidColorBrush(WhiteColor);

        private GeometryGroup _geometry = null;
        private Path _path = null;

        public FunctionPlotter()
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
        }

        public void Plot(double[] values)
        {
            double min = values.Min();
            double max = values.Max();
            double lastReading = double.NaN;
            double length = (double)values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                // Normalize the value to the vertical bounds
                var curReading = (-((values[i] - min) / (max - min) - 0.5) + 0.5) * (ActualHeight);

                // Initialize the first reading
                if (lastReading == double.NaN)
                    lastReading = curReading;

                // Add new path segment
                _geometry.Children.Add(new LineGeometry()
                {
                    StartPoint = new System.Windows.Point(((double)i - 1) / length * ActualWidth, lastReading),
                    EndPoint = new System.Windows.Point(((double)i) / length * ActualWidth, curReading)
                });

                lastReading = curReading;
            }
        }
    }
}
