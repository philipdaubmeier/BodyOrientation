using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BodyOrientationLib
{
    public class LearnerPredictedFeatureSet : AbstractFeatureSet
    {
        public const int NumValues = 4;
        public override int NumMultiplexableItems { get { return NumValues; } }

        public double PredictedLeftLegAngle { get; set; }
        public double PredictedRightLegAngle { get; set; }
        public double PredictedPersonHeading { get; set; }

        public bool IsLearned { get; set; }

        public LearnerPredictedFeatureSet() { }

        public void ReadFromLearnerResults(double[] responses)
        {
            PredictedLeftLegAngle = responses[0];
            PredictedRightLegAngle = responses[1];
            PredictedPersonHeading = responses[2];
        }

        public override void WriteToRecorder(BinaryWriter writer)
        {
            writer.Write(PredictedLeftLegAngle);
            writer.Write(PredictedRightLegAngle);
            writer.Write(PredictedPersonHeading);
            writer.Write(IsLearned ? 1d : 0d);
        }

        public override void ReadFromRecorder(BinaryReader reader)
        {
            PredictedLeftLegAngle = reader.ReadDouble();
            PredictedRightLegAngle = reader.ReadDouble();
            PredictedPersonHeading = reader.ReadDouble();
            IsLearned = reader.ReadDouble() == 1d;
        }

        public override object Clone()
        {
            return new LearnerPredictedFeatureSet()
            {
                PredictedLeftLegAngle = this.PredictedLeftLegAngle,
                PredictedRightLegAngle = this.PredictedRightLegAngle,
                PredictedPersonHeading = this.PredictedPersonHeading,
                IsLearned = this.IsLearned
            };
        }
    }
}
