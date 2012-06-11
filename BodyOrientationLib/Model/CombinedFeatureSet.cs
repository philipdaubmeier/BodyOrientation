using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Research.Kinect.Nui;

namespace BodyOrientationLib
{
    public class CombinedFeatureSet : IRecordable
    {
        public SensorRawFeatureSet RawSensors { get; set; }
        public KinectRawFeatureSet RawKinect { get; set; }
        public ManualRawFeatureSet RawManual { get; set; }
        public SensorFeatureSet SensorFeatures { get; set; }
        public KinectFeatureSet KinectFeatures { get; set; }
        public LearnerPredictedFeatureSet LearnerFeatures { get; set; }

        public CombinedFeatureSet() { }

        public void WriteToRecorder(BinaryWriter writer)
        {
            // Assure that we always write the same amount, even if its empty
            (RawSensors ?? new SensorRawFeatureSet()).WriteToRecorder(writer);
            (RawKinect ?? new KinectRawFeatureSet()).WriteToRecorder(writer);
            (RawManual ?? new ManualRawFeatureSet()).WriteToRecorder(writer);

            // Only write and read raw features
            // (SensorFeatures ?? new SensorFeatureSet()).WriteToRecorder(writer);
            // (KinectFeatures ?? new KinectFeatureSet()).WriteToRecorder(writer);
        }

        public void ReadFromRecorder(BinaryReader reader)
        {
            RawSensors = new SensorRawFeatureSet();
            RawKinect = new KinectRawFeatureSet();
            RawManual = new ManualRawFeatureSet();

            RawSensors.ReadFromRecorder(reader);
            RawKinect.ReadFromRecorder(reader);
            RawManual.ReadFromRecorder(reader);

            // Only write and read raw features
            // SensorFeatures = new SensorFeatureSet();
            // KinectFeatures = new KinectFeatureSet();
            // SensorFeatures.ReadFromRecorder(reader);
            // KinectFeatures.ReadFromRecorder(reader);
        }
    }
}
