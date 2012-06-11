using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BodyOrientationLib
{
    public class SensorFeatureSet : AbstractFeatureSet
    {
        public const int NumValues = 15;
        public override int NumMultiplexableItems { get { return NumValues; } }

        //TODO add to all methods
        public double Heading { get; set; }

        public double RotationX { get; set; }
        public double RotationY { get; set; }
        public double RotationZ { get; set; }

        public double RotationXMean { get; set; }
        public double RotationYMean { get; set; }
        public double RotationZMean { get; set; }

        public double RotationXStdDev { get; set; }
        public double RotationYStdDev { get; set; }
        public double RotationZStdDev { get; set; }

        public double RotationCorrelationXY { get; set; }
        public double RotationCorrelationXZ { get; set; }
        public double RotationCorrelationYZ { get; set; }

        public double RotationXEnergy { get; set; }
        public double RotationYEnergy { get; set; }
        public double RotationZEnergy { get; set; }


        public void ReadFromAnalysisResult(AnalysisResult analysisresult)
        {
            RotationX = analysisresult.CurrentValues[0];
            RotationY = analysisresult.CurrentValues[1];
            RotationZ = analysisresult.CurrentValues[2];

            RotationXMean = analysisresult.Means[0];
            RotationYMean = analysisresult.Means[1];
            RotationZMean = analysisresult.Means[2];

            RotationXStdDev = analysisresult.StandardDeviations[0];
            RotationYStdDev = analysisresult.StandardDeviations[1];
            RotationZStdDev = analysisresult.StandardDeviations[2];

            RotationCorrelationXY = analysisresult.CorrelationMatrix[0, 1];
            RotationCorrelationXZ = analysisresult.CorrelationMatrix[0, 2];
            RotationCorrelationYZ = analysisresult.CorrelationMatrix[1, 2];

            RotationXEnergy = analysisresult.Energies[0];
            RotationYEnergy = analysisresult.Energies[1];
            RotationZEnergy = analysisresult.Energies[2];
        }

        public override double[] ExtractValues()
        {
            double[] values = new double[NumMultiplexableItems];
            values[0] = RotationX;
            values[1] = RotationY;
            values[2] = RotationZ;

            values[3] = RotationXMean;
            values[4] = RotationYMean;
            values[5] = RotationZMean;

            values[6] = RotationXStdDev;
            values[7] = RotationYStdDev;
            values[8] = RotationZStdDev;

            values[9] = RotationCorrelationXY;
            values[10] = RotationCorrelationXZ;
            values[11] = RotationCorrelationYZ;

            values[12] = RotationXEnergy;
            values[13] = RotationYEnergy;
            values[14] = RotationZEnergy;
            return values;
        }

        public override void InjectValues(double[] values)
        {
            RotationX = values[0];
            RotationY = values[1];
            RotationZ = values[2];

            RotationXMean = values[3];
            RotationYMean = values[4];
            RotationZMean = values[5];

            RotationXStdDev = values[6];
            RotationYStdDev = values[7];
            RotationZStdDev = values[8];

            RotationCorrelationXY = values[9];
            RotationCorrelationXZ = values[10];
            RotationCorrelationYZ = values[11];

            RotationXEnergy = values[12];
            RotationYEnergy = values[13];
            RotationZEnergy = values[14];
        }

        public override object Clone() { return base.CloneByValues<SensorFeatureSet>(); }
    }
}
