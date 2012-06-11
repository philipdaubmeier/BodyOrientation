using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BodyOrientationLib
{
    /// <summary>
    /// Two-in-one multiplexing and sequence processing unit. It handles two seperate sensor
    /// input streams (from two phones) and multiplexes them into one stream of 
    /// SensorComparisonFeatureSet objects. The ItemMultiplexed event is raised each 
    /// time such a new object is ready. Additionally it processes the raw inputs and 
    /// extracts all sensor features from them.
    /// </summary>
    public class SensorComparisonMultiplexer : Multiplexer<SensorComparisonFeatureSet>
    {
        private const int defaultAnalysisWindowSize = 32;

        private SequenceAnalyzer analyzer1 = null;
        private SequenceAnalyzer analyzer2 = null;

        public SensorComparisonMultiplexer() : this(SensorComparisonMultiplexer.defaultAnalysisWindowSize) { }

        public SensorComparisonMultiplexer(int analysisWindowSize) : this(analysisWindowSize, 0, 0) { }

        // Pass metadata - needed for multiplexing - to base class
        public SensorComparisonMultiplexer(int analysisWindowSize, int sensor1TimeOffset, int sensor2TimeOffset)
            : base(InterpolationMethod.None, new StreamInfo[]{
                new StreamInfo(){
                    StreamId = 0,
                    Name = "Raw sensor values 1",
                    NumValues = SensorRawFeatureSet.NumValues,
                    TimeOffsetMilliseconds = sensor1TimeOffset
                },
                new StreamInfo(){
                    StreamId = 1,
                    Name = "Raw sensor values 2",
                    NumValues = SensorRawFeatureSet.NumValues,
                    TimeOffsetMilliseconds = sensor2TimeOffset
                }
        })
        {
            analyzer1 = new SequenceAnalyzer(3, analysisWindowSize);
            analyzer2 = new SequenceAnalyzer(3, analysisWindowSize);
        }

        public void PushRawSensor1Values(SensorRawFeatureSet item) { base.Push(0, item); }
        public void PushRawSensor2Values(SensorRawFeatureSet item) { base.Push(1, item); }

        protected override SensorComparisonFeatureSet PackMultiplexItem(IMultiplexable[] values)
        {
            var rawSensor1 = (SensorRawFeatureSet)values[0] ?? new SensorRawFeatureSet();
            var rawSensor2 = (SensorRawFeatureSet)values[1] ?? new SensorRawFeatureSet();
            var sensor1Features = new SensorFeatureSet();
            var sensor2Features = new SensorFeatureSet();

            // Process through Sequence analyzer (extracts statistical features over the last x values)
            // and fill into SensorFeatureSet
            //
            // Attention: the streams of those sensorvalues are not the original streams from the phone!
            // These are the multiplexed streams. This means, some values could be omitted or could have 
            // been cloned due to syncronizing all streams, or even be interpolated between original values.
            var analysisResults1 = analyzer1.NextValues(rawSensor1.RotationPitch, rawSensor1.RotationRoll, rawSensor1.RotationYaw);
            var analysisResults2 = analyzer1.NextValues(rawSensor2.RotationPitch, rawSensor2.RotationRoll, rawSensor2.RotationYaw);
            sensor1Features.ReadFromAnalysisResult(analysisResults1);
            sensor2Features.ReadFromAnalysisResult(analysisResults2);

            // Pack all single sets into a combined set
            return new SensorComparisonFeatureSet()
            {
                RawSensors1 = rawSensor1,
                RawSensors2 = rawSensor2,
                SensorFeatures1 = sensor1Features,
                SensorFeatures2 = sensor2Features
            };
        }
    }
}
