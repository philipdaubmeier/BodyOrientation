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
        public class CalibrationAngleCalculatedEventArgs : EventArgs
        {
            public double CalibrationAngle { get; set; }
        }

        private const int defaultAnalysisWindowSize = 32;

        private SequenceAnalyzer analyzer = null;

        private SupervisedLearner learner = null;

        public event EventHandler<CalibrationAngleCalculatedEventArgs> CalibrationAngleCalculated;

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
            // Transform the quaternion rotation into a matrix, due to performance reasons
            var rotationMatrix = Matrix3D.Identity;
            rotationMatrix.Rotate(rawSensor.Quat);
            
            // Start with the characteristical unit vectors
            var vecBottomToHead = new Vector3D(0, 1, 0);
            var vecThroughDisplay = new Vector3D(0, 0, -1);

            // Rotate them around given the rotation matrix, constructed from the quaternion
            var vectors = new Vector3D[] { vecBottomToHead, vecThroughDisplay };
            rotationMatrix.Transform(vectors);
            vecBottomToHead = vectors[0];
            vecThroughDisplay = vectors[1];

            // Get the weight by using the euclidean norm of the projection of one unit vector
            var weight = vecBottomToHead.ProjectToGround().Length;
            
            // Adjust weight with logistic function
            weight = InterpolationLogisticsFunction(weight);

            // Get the angle by interpolating between the two candidate angles, 
            // weighted by the adjusted weight. Interpolate over the shorter 
            // path around the circle with lerping vectors first then get the angle.
            var angle = LerpVectors(vecBottomToHead, vecThroughDisplay, weight).AngleOnGround();
            
            // Old method: calculated angles first, then interpolated via a special 
            // cyclic lerp function. This is equivalent because it is basically a 
            // transformation to polar coordinates instead lerping in cartesian 
            // coordinates, but not as performant as the method is now.
            //
            //    var angleA = vecBottomToHead.AngleOnGround();
            //    var angleB = vecThroughDisplay.AngleOnGround();
            //    var angle = LerpAnglesRadians(angleB, angleA, weight);

            // The angle is then turned around 180°, as the phone is assumed to face
            // to the back, and we want an angle similar to the where the user is faced. 
            // Add the offset from the calibration to it to compensate for the Kinect
            // not facing northwards, but any direction.
            angle = ApplyAngleOffset(TurnAround(angle), rawManual.CalibrationAngle);

            // If the calibration just happened and the quaternion for the calibration
            // was set, but not yet the calculated calibration angle, set it now.
            if (!rawManual.CalibrationQuaternion.IsIdentity && rawManual.CalibrationAngle == 0d)
            {
                if (CalibrationAngleCalculated != null)
                    CalibrationAngleCalculated(this, new CalibrationAngleCalculatedEventArgs() { CalibrationAngle = -angle });
                angle = 0;
            }

            // Set the final angle as the heading information from the sensor
            sensorFeatures.Heading = angle;
            
            // TODO: reconstruct user heading with: 
            // userHeading = sensorAngle - estimatedDelta 
            //             = sensorAngle - (sensorAngle - ShoulderOrientation)
            //             = ShoulderOrientation



            // Applying the heading rotation to the quaternion to make the phones movement 
            // independent of the heading. Very important step for transforming into a
            // machine learnable stream!
            ApplyCalibrationAngle(rawSensor, -angle);


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

            // Substract the kinect heading from the phone heading to get the heading delta
            sensorFeatures.HeadingDelta = sensorFeatures.Heading - kinectFeatures.ShoulderOrientation;

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

        private void ApplyCalibrationAngle(SensorRawFeatureSet rawSensor, double angle)
        {
            ApplyCalibrationQuaternion(rawSensor, new Quaternion(new Vector3D(0, 0, 1), angle.ToDegrees()));
        }

        /// <summary>
        /// A specially adjusted version of the general logistics function
        /// 
        ///     y = 1 / (1 + exp(-x))
        ///     
        /// that is not centered around 0, but around 0.5 and is designed to take
        /// values in the interval of [0, 1] and return values between ]0, 1[.
        /// This function can be used for interpolation between two values, 
        /// where a linear interpolation would have to much weight on the farther
        /// located value. For example, linear interpolation between values a, b
        /// with weight w:
        /// 
        ///     a = 1
        ///     b = 1000
        ///     w = 95% (towards a)
        ///     result = (a * w) + (b * (1 - w)) = 0,95 + 50 = 50,95
        ///     
        /// it can be seen that 50,95 is exactly linearly in between those two 
        /// values with the given weight, but far larger than expected in some
        /// applications, because the value of b is that large. It should really
        /// be very near to 1. Using this logistics function, the weight can be
        /// adjusted to achieve this behaviour, as it maps the [0, 1] interval of
        /// valid interpolation weights (not regarding extrapolation) onto the
        /// same interval again, but giving values near 0 or near 1 a stronger
        /// weight. The example again with the logistics function, and the same
        /// linear interpolation function:
        /// 
        ///     w = InterpolationLogisticsFunction(95%) = 99,9254%
        ///     result = (a * w) + (b * (1 - w)) = 0,999 + 0,746 = 1,745
        /// 
        /// which is much closer to 1, and closer to what was intended. The 
        /// function was hardcoded to a constant scaling factor of 16.
        /// The factor 16 was chosen because it showed, the factors in the range 
        /// of 12 to 18 proved to be suitable for this purpose. Is a trade-off 
        /// between the slope in between the function and the actual benefitial 
        /// weight that is added by applying the function. With chosing smaller values,
        /// the function becomes very similar to the identity map y = x, which
        /// nullifies the purpose of the whole process. Larger values for the scaling
        /// factor lets it approximate a function that cuts input values
        /// at a threshold of 0.5 and returns only values very near to 0 or 1. This,
        /// in turn, nullifies the need for an interpolation altogether.
        /// The factor 16 was therefore chosen, as it lies in between those boundaries 
        /// and has the advantage of being a power of two. This way it allows
        /// for easy multiplication with the value, as the floating point
        /// representation in current computers is based on a binary mantissa and 
        /// exponent. The addition of 4 to the values exponent is equivalent 
        /// to the multiplication of the number with 16, moreover leaving the 
        /// mantissa untouched and avoiding numerical errors completely.
        /// The function is therefore described by:
        /// 
        ///     y = 1 / (1 + exp(-(x - 0.5) * 16))
        /// 
        /// Exact values of 0 or 1 will likely never be returned, as these are
        /// just the assymptotes of the function. However, due to computational
        /// inaccuracies this may happen for very small or large input values.
        /// All input values are allowed, even -Inf or Inf. Note, that an input
        /// value of double.NaN will return an output of NaN.
        /// </summary>
        /// <param name="value">The input value between 0 and 1 (both inclusive).</param>
        /// <returns>The output value between 0 and 1 (both inclusive).</returns>
        private double InterpolationLogisticsFunction(double value){
            const int ScalingFactor = 16;
            return 1 / (1 + Math.Exp(-(value - 0.5) * ScalingFactor));
        }

        /// <summary>
        /// Turns around a given angle in radians by 180°.
        /// It is assumed the input angle is inside the range ]-pi, pi].
        /// Note: This is not checked for performance reasons. If not inside 
        /// this range, it is not assured the function returns the output
        /// angle inside this range!
        /// </summary>
        /// <param name="angle">Angle in radians between -pi and pi.</param>
        /// <returns>The turned-around resulting angle inside the range ]-pi, pi]</returns>
        private double TurnAround(double angle){
            return angle > 0 ? angle - Math.PI : angle + Math.PI;
        }

        private double ApplyAngleOffset(double angle, double offset)
        {
            angle += offset;
            return angle > Math.PI ? angle - 2 * Math.PI : (angle < -Math.PI ? angle + 2 * Math.PI : angle);
        }

        private Vector3D LerpVectors(Vector3D start, Vector3D end, double amount)
        {
            return start * amount + end * (1 - amount);
        }

        private double LerpAnglesRadians(double start, double end, double amount)
        {
            var difference = Math.Abs(end - start);
            if (difference > Math.PI)
            {
                if (end > start)
                    start += Math.PI * 2;
                else
                    end += Math.PI * 2;
            }

            // Interpolate
            var value = start + ((end - start) * amount);

            // Wrap around the circle
            if (value < -Math.PI)
                return value + Math.PI * 2;
            else if (value > Math.PI)
                return value - Math.PI * 2;
            else
                return value;
        }
    }
}
