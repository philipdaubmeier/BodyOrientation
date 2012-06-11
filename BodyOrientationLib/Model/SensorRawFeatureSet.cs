using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Media3D;

namespace BodyOrientationLib
{
    public class SensorRawFeatureSet : AbstractFeatureSet, ISensorReading
    {
        public const int NumValues = 26;
        public override int NumMultiplexableItems { get { return NumValues; } }

        public int NumSensorValues { get { return 23; } }

        public Quaternion Quat
        {
            get
            {
                return new Quaternion(QuaternionX, QuaternionY, QuaternionZ, QuaternionW);
            }
            set
            {
                double x = value.X;
                double y = value.Y;
                double z = value.Z;
                double w = value.W;

                QuaternionX = x;
                QuaternionY = y;
                QuaternionZ = z;
                QuaternionW = w;

                RotationPitch = Math.Atan2(2 * (y * z + w * x), w * w - x * x - y * y + z * z);
                RotationRoll = Math.Atan2(2 * (x * y + w * z), w * w + x * x - y * y - z * z);
                RotationYaw = Math.Asin(-2 * (x * z - w * y));
            }
        }

        public double QuaternionX { get; set; }
        public double QuaternionY { get; set; }
        public double QuaternionZ { get; set; }
        public double QuaternionW { get; set; }

        public double RotationPitch { get; set; }
        public double RotationRoll { get; set; }
        public double RotationYaw { get; set; }

        public double RotationRateX { get; set; }
        public double RotationRateY { get; set; }
        public double RotationRateZ { get; set; }

        public double RawAccelerationX { get; set; }
        public double RawAccelerationY { get; set; }
        public double RawAccelerationZ { get; set; }

        public double LinearAccelerationX { get; set; }
        public double LinearAccelerationY { get; set; }
        public double LinearAccelerationZ { get; set; }

        public double GravityX { get; set; }
        public double GravityY { get; set; }
        public double GravityZ { get; set; }

        public double MagneticHeading { get; set; }
        public double TrueHeading { get; set; }
        public double HeadingAccuracy { get; set; }

        public double MagnetometerX { get; set; }
        public double MagnetometerY { get; set; }
        public double MagnetometerZ { get; set; }
        public bool MagnetometerDataValid { get; set; }

        public void SetSensorValues(float[] values)
        {
            if (values == null || values.Length != NumSensorValues)
                throw new ArgumentException("Unexpected length of array.");

            double x = (double)values[0];
            double y = (double)values[1];
            double z = (double)values[2];
            double w = (double)values[3];

            double[] newvalues = new double[26];
            for (int i = 0; i < 23; i++)
                newvalues[i] = (double)values[i];

            // TODO: rework and use the setter of the quaternion, also change the InjectValues method
            newvalues[23] = Math.Atan2(2 * (y * z + w * x), w * w - x * x - y * y + z * z);
            newvalues[24] = Math.Atan2(2 * (x * y + w * z), w * w + x * x - y * y - z * z);
            newvalues[25] = Math.Asin(-2 * (x * z - w * y));

            InjectValues(newvalues);
        }

        public override double[] ExtractValues()
        {
            double[] values = new double[NumMultiplexableItems];
            values[0] = QuaternionX;
            values[1] = QuaternionY;
            values[2] = QuaternionZ;
            values[3] = QuaternionW;

            values[7] = RotationRateX;
            values[8] = RotationRateY;
            values[9] = RotationRateZ;

            values[13] = RawAccelerationX;
            values[14] = RawAccelerationY;
            values[15] = RawAccelerationZ;

            values[4] = LinearAccelerationX;
            values[5] = LinearAccelerationY;
            values[6] = LinearAccelerationZ;

            values[10] = GravityX;
            values[11] = GravityY;
            values[12] = GravityZ;

            values[16] = MagneticHeading;
            values[17] = TrueHeading;
            values[18] = HeadingAccuracy;

            values[19] = MagnetometerX;
            values[20] = MagnetometerY;
            values[21] = MagnetometerZ;
            values[22] = MagnetometerDataValid ? 1d : 0d;

            values[23] = RotationPitch;
            values[24] = RotationRoll;
            values[25] = RotationYaw;
            return values;
        }

        public override void InjectValues(double[] values)
        {
            QuaternionX = values[0];
            QuaternionY = values[1];
            QuaternionZ = values[2];
            QuaternionW = values[3];

            RotationRateX = values[7];
            RotationRateY = values[8];
            RotationRateZ = values[9];

            RawAccelerationX = values[13];
            RawAccelerationY = values[14];
            RawAccelerationZ = values[15];

            LinearAccelerationX = values[4];
            LinearAccelerationY = values[5];
            LinearAccelerationZ = values[6];

            GravityX = values[10];
            GravityY = values[11];
            GravityZ = values[12];

            MagneticHeading = values[16];
            TrueHeading = values[17];
            HeadingAccuracy = values[18];

            MagnetometerX = values[19];
            MagnetometerY = values[20];
            MagnetometerZ = values[21];
            MagnetometerDataValid = values[22] == 1d;

            RotationPitch = values[23];
            RotationRoll = values[24];
            RotationYaw = values[25];
        }

        public override object Clone() { return base.CloneByValues<SensorRawFeatureSet>(); }
    }
}
