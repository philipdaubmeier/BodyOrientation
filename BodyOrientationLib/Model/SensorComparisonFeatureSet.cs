using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Research.Kinect.Nui;

namespace BodyOrientationLib
{
    public class SensorComparisonFeatureSet : IRecordable
    {
        public SensorRawFeatureSet RawSensors1 { get; set; }
        public SensorRawFeatureSet RawSensors2 { get; set; }
        public SensorFeatureSet SensorFeatures1 { get; set; }
        public SensorFeatureSet SensorFeatures2 { get; set; }

        public SensorComparisonFeatureSet() { }

        public void WriteToRecorder(BinaryWriter writer)
        {
            // Assure that we always write the same amount, even if its empty
            (RawSensors1 ?? new SensorRawFeatureSet()).WriteToRecorder(writer);
            (RawSensors2 ?? new SensorRawFeatureSet()).WriteToRecorder(writer);
        }

        public void ReadFromRecorder(BinaryReader reader)
        {
            RawSensors1 = new SensorRawFeatureSet();
            RawSensors2 = new SensorRawFeatureSet();

            RawSensors1.ReadFromRecorder(reader);
            RawSensors2.ReadFromRecorder(reader);
        }
    }
}
