using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BodyOrientationLib;
using System.Windows.Media.Media3D;

namespace BodyOrientationControlLib
{
    public class CalibratedEventArgs : EventArgs
    {
        public Quaternion CalibrationQuaternion { get; set; }
    }

    public partial class PhoneModel : UserControl
    {
        public event EventHandler<CalibratedEventArgs> Calibrated;

        public bool CalibrationEnabled
        {
            get
            {
                return this.buttonCalibrate.Visibility == Visibility.Visible;
            }
            set
            {
                this.buttonCalibrate.Visibility = value ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public PhoneModel()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(PhoneModelLoaded);
        }

        private void PhoneModelLoaded(object sender, RoutedEventArgs e)
        {
            buttonCalibrate.Click += (s, ea) => { calibrate = true; };

            // Only for debugging: uncomment for colored axis indicators
            //Phone.Children.AddCube(1, 0, 0, 0.1, Color.FromRgb(255, 0, 0));
            //Phone.Children.AddCube(0, 1, 0, 0.1, Color.FromRgb(0, 255, 0));
            //Phone.Children.AddCube(0, 0, 1, 0.1, Color.FromRgb(0, 0, 255));

            Phone.Freeze();
        }

        private bool calibrate = false;
        public void Update3dPhoneModel(SensorRawFeatureSet reading)
        {
            var quat = new Quaternion(reading.QuaternionX, reading.QuaternionY,
                                      reading.QuaternionZ, reading.QuaternionW);
            GlobalSystem.Transform = new RotateTransform3D(new QuaternionRotation3D(quat));

            if (calibrate)
            {
                quat.Invert();
                CalibrateManually(quat);
                calibrate = false;
            }
        }

        public void CalibrateManually(Quaternion quaternion)
        {
            //CalibrationSystem.Transform = new RotateTransform3D(new QuaternionRotation3D(quaternion));

            // Fire event that calibration happened
            if (Calibrated != null)
                Calibrated(this, new CalibratedEventArgs() { CalibrationQuaternion = quaternion });
        }
    }
}
