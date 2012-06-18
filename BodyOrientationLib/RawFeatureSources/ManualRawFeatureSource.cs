using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace BodyOrientationLib
{
    public class ManualRawFeatureSource : AbstractRawFeatureSource<ManualRawFeatureSet>
    {
        protected override string defaultRecordingPath { get { return @"C:\manualsavedata.msd"; } }

        private int phase = 0;
        private Posture[] phases = null;

        private Quaternion calibrationQuaternion = new Quaternion();
        private double calibrationAngle = 0d;

        public ManualRawFeatureSource(Mode sourceMode) : this(sourceMode, null) { }

        public ManualRawFeatureSource(Mode sourceMode, string recordingPath) : base(sourceMode, recordingPath) { }

        protected override void Initialize(object constructorData) { }


        protected override void StartLiveStream()
        {
            phase = 0;
            phases = new Posture[]{
                new Posture(Posture.State.NotClassified),
                new Posture(Posture.Stable.Sitting),
                new Posture(Posture.Transitions.StandingUp),
                new Posture(Posture.Stable.Standing),
                new Posture(Posture.Stable.Walking),
                new Posture(Posture.Stable.Standing),
                new Posture(Posture.Transitions.SittingDown),
                new Posture(Posture.Stable.Sitting),
                new Posture(Posture.Transitions.StandingUp),
                new Posture(Posture.Stable.Standing),
                new Posture(Posture.Transitions.SittingDown),
                new Posture(Posture.Stable.Sitting),
                new Posture(Posture.State.NotClassified)
            };
        }

        public void NextPhase(){
            if (phases != null)
            {
                phase = (phase + 1) % phases.Length;

                PushOutNextItem();
            }
        }

        public void SetNewCalibration(Quaternion quaternion)
        {
            calibrationQuaternion = quaternion;

            PushOutNextItem();
        }

        public void SetNewCalibrationAngle(double angle)
        {
            calibrationAngle = angle;

            PushOutNextItem();
        }

        private void PushOutNextItem()
        {
            var manualFeatures = new ManualRawFeatureSet()
            {
                BodyPosture = phases[phase],
                NextPosture = phases[(phase + 1) % phases.Length],
                CalibrationQuaternion = calibrationQuaternion,
                CalibrationAngle = calibrationAngle
            };

            if (sourceMode == Mode.UseLiveStreamAndRecordIt)
                recorder.RecordFrame(manualFeatures);

            OnNewItem(manualFeatures);
        }
    }
}
