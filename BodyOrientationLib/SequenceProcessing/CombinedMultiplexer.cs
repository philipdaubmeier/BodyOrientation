using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Diagnostics;

namespace BodyOrientationLib
{
    /// <summary>
    /// Three-in-one multiplexing and sequence processing unit: Multiplexing, Feature Extraction 
    /// and Machine Learning execution.
    /// 
    /// Multiplexing:
    /// =============
    /// It handles three seperate input streams (raw sensor values, raw kinect skeleton data and 
    /// raw manual user input) and multiplexes them into one stream of CombinedFeatureSet objects. 
    /// The ItemMultiplexed event is raised each time such a new object is ready.
    /// 
    /// Feature Extraction:
    /// ===================
    /// Additionally it processes the raw inputs and extracts features from them to 
    /// process further:
    ///  - Raw sensor values are put into a sequence analyzer for extraction of
    ///    relevant statistical features. The results are packed into SensorFeatureSet
    ///    object, which is in turn packed into the CombinedFeatureSet object.
    ///  - Raw kinect skeleton data is ran through the calculations of the KinectFeatureSet,
    ///    which is then also included in the CombinedFeatureSet object.
    ///    
    /// Machine Learning:
    /// =================
    /// Furthermore, it executes the supervised machine learning via a SupervisedLearner
    /// object. It passes the current values and features of this timeframe into the learner
    /// to either feed it with learning data (unlearned state) for model fitting or gather 
    /// the results of the fitted model (learned state).
    /// </summary>
    public class CombinedMultiplexer : Multiplexer<CombinedFeatureSet>
    {
        private const int defaultAnalysisWindowSize = 32;

        private SequenceAnalyzer analyzer = null;

        private SupervisedLearner learner = null;

        public CombinedMultiplexer() : this(CombinedMultiplexer.defaultAnalysisWindowSize) { }

        public CombinedMultiplexer(int analysisWindowSize) : this(analysisWindowSize, 0, 0, 0) { }

        // Pass metadata - needed for multiplexing - to base class
        public CombinedMultiplexer(int analysisWindowSize, int sensorTimeOffset, int kinectTimeOffset, int manualDataTimeOffset)
            : base(InterpolationMethod.None, new StreamInfo[]{
                new StreamInfo(){
                    StreamId = 0,
                    Name = "Raw sensor values",
                    NumValues = SensorRawFeatureSet.NumValues,
                    TimeOffsetMilliseconds = sensorTimeOffset
                },
                new StreamInfo(){
                    StreamId = 1,
                    Name = "Raw kinect values",
                    NumValues = KinectRawFeatureSet.NumValues,
                    TimeOffsetMilliseconds = kinectTimeOffset
                },
                new StreamInfo(){
                    StreamId = 2,
                    Name = "Raw manual values",
                    NumValues = ManualRawFeatureSet.NumValues,
                    TimeOffsetMilliseconds = manualDataTimeOffset
                }
        })
        {
            analyzer = new SequenceAnalyzer(3, analysisWindowSize);

            learner = new SupervisedLearner();
        }

        public void PushRawSensorValues(SensorRawFeatureSet item) { base.Push(0, item); }
        public void PushRawKinectValues(KinectRawFeatureSet item) { base.Push(1, item); }
        public void PushRawManualValues(ManualRawFeatureSet item) { base.Push(2, item); }

        /// <summary>
        /// A multiplexed time frame is passed to this method, if pending. Here, the sequence
        /// analysis and feature extraction is done, as well as the feeding of machine learning 
        /// data to the learner.
        /// </summary>
        /// <param name="values">Objects of the different streams that were multiplexed</param>
        /// <returns>A CombinedFeatureSet containing all processed and multiplexed data</returns>
        protected override CombinedFeatureSet PackMultiplexItem(IMultiplexable[] values)
        {
            var rawSensor = (SensorRawFeatureSet)values[0] ?? new SensorRawFeatureSet();
            var rawKinect = (KinectRawFeatureSet)values[1] ?? new KinectRawFeatureSet();
            var rawManual = (ManualRawFeatureSet)values[2] ?? new ManualRawFeatureSet();
            var sensorFeatures = new SensorFeatureSet();
            var kinectFeatures = new KinectFeatureSet();
            var learnerFeatures = new LearnerPredictedFeatureSet();


            // TODO: outsource this. this is just quick-and-dirty inside this function
            var rotationMatrix = Matrix3D.Identity;
            rotationMatrix.Rotate(rawSensor.Quat);
            
            var vecBottomToHead = new Vector3D(0, 1, 0);
            var vecThroughDisplay = new Vector3D(0, 0, -1);

            var vectors = new Vector3D[] { vecBottomToHead, vecThroughDisplay };
            rotationMatrix.Transform(vectors);
            vecBottomToHead = vectors[0];
            vecThroughDisplay = vectors[1];

            var weight = vecBottomToHead.ProjectToGround().Length;
            
            var angleA = vecBottomToHead.AngleOnGround();
            var angleB = vecThroughDisplay.AngleOnGround();

            //TODO interpolate over the shorter path around the circle
            sensorFeatures.Heading = weight * angleA + (1 - weight) * angleB;


            //TODO: right place here?
            ApplyCalibrationQuaternion(rawSensor, rawManual.CalibrationQuaternion);


            // Process through Sequence analyzer (extracts statistical features over the last x values)
            // and fill into SensorFeatureSet
            //
            // Attention: the stream of those sensorvalues is not the original stream from the phone!
            // This is the multiplexed stream. This means, some values could be omitted or could have 
            // been cloned due to syncronizing all streams, or even be interpolated between original values.
            var analysisResults = analyzer.NextValues(rawSensor.RotationPitch, rawSensor.RotationRoll, rawSensor.RotationYaw);
            sensorFeatures.ReadFromAnalysisResult(analysisResults);

            // Process skeleton joints by passing it into the KinectFeatureSet
            var skeletonJoints = rawKinect == null ? null : rawKinect.SkeletonJoints;
            kinectFeatures.ReadFromSkeletonJoints(skeletonJoints);

            // Process through Supervised learner
            learnerFeatures.ReadFromLearnerResults(learner.FeedValues(
                                new double[]{
                                    kinectFeatures.LeftLegToTorsoAngle,
                                    kinectFeatures.RightLegToTorsoAngle,
                                    kinectFeatures.ShoulderOrientation
                                },
                                sensorFeatures.RotationX,
                                sensorFeatures.RotationY,
                                sensorFeatures.RotationZ,
                                sensorFeatures.RotationXMean,
                                sensorFeatures.RotationYMean,
                                sensorFeatures.RotationZMean,
                                sensorFeatures.RotationXStdDev,
                                sensorFeatures.RotationYStdDev,
                                sensorFeatures.RotationZStdDev,
                                sensorFeatures.RotationCorrelationXY,
                                sensorFeatures.RotationCorrelationXZ,
                                sensorFeatures.RotationCorrelationYZ,
                                sensorFeatures.RotationXEnergy,
                                sensorFeatures.RotationYEnergy,
                                sensorFeatures.RotationZEnergy,
                                rawSensor.RotationRateX,
                                rawSensor.RotationRateY,
                                rawSensor.RotationRateZ
                                ));
            learnerFeatures.IsLearned = learner.IsLearned;

            // Pack all single sets into a combined set
            return new CombinedFeatureSet()
            {
                RawSensors = rawSensor,
                RawKinect = rawKinect,
                RawManual = rawManual,
                SensorFeatures = sensorFeatures,
                KinectFeatures = kinectFeatures,
                LearnerFeatures = learnerFeatures
            };
        }

        private void ApplyCalibrationQuaternion(SensorRawFeatureSet rawSensor, Quaternion quat)
        {
            // Attention: non-commutative quaternion multiplication. Dont switch order!
            rawSensor.Quat = quat * rawSensor.Quat;
        }
    }
}
