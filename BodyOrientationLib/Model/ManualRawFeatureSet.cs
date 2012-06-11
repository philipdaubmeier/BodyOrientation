using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Media3D;

namespace BodyOrientationLib
{
    public class ManualRawFeatureSet : AbstractFeatureSet
    {
        public const int NumValues = 0;
        public override int NumMultiplexableItems { get { return NumValues; } }

        /// <summary>
        /// Manually classified body posture. 
        /// 
        /// Can hold four basic states, with two of them further divided:
        ///  - Not classified
        ///  - Not on body (not able to classify body posture, because sensors are not on body)
        ///  - Some transitioning state (further divided into several concrete 
        ///                              transitioning states, such as: standing up, sitting down, ...)
        ///  - Some stable state (further divided into several concrete stable 
        ///                       states, such as: standing, sitting, walking, ...)
        /// </summary>
        public Posture BodyPosture { get; set; }

        public Posture NextPosture { get; set; }

        public Quaternion CalibrationQuaternion { get; set; }

        public ManualRawFeatureSet()
        {
            BodyPosture = new Posture(Posture.State.NotClassified);
            NextPosture = BodyPosture;
        }

        public override void WriteToRecorder(BinaryWriter writer)
        {
            writer.Write(BodyPosture.GetId());
            //TODO: write quat
        }

        public override void ReadFromRecorder(BinaryReader reader)
        {
            BodyPosture = Posture.FromId(reader.ReadInt32());
            //TODO: read quat
        }

        public override object Clone()
        {
            return new ManualRawFeatureSet()
            {
                BodyPosture = this.BodyPosture,
                NextPosture = this.NextPosture,
                CalibrationQuaternion = this.CalibrationQuaternion
            };
        }
    }
}
