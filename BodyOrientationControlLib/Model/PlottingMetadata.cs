using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BodyOrientationLib;

namespace BodyOrientationControlLib
{
    public static class PlottingMetadata
    {
        private static PlottableValue<SensorRawFeatureSet>[] sensorRawValues = new PlottableValue<SensorRawFeatureSet>[]{
            new PlottableValue<SensorRawFeatureSet>("Quaternion X", v => "Quaternion X: " + v.ToString("0.0"), s => s.QuaternionX, 0, -1, 1),
            new PlottableValue<SensorRawFeatureSet>("Quaternion Y", v => "Quaternion Y: " + v.ToString("0.0"), s => s.QuaternionY, 0, -1, 1),
            new PlottableValue<SensorRawFeatureSet>("Quaternion Z", v => "Quaternion Z: " + v.ToString("0.0"), s => s.QuaternionZ, 0, -1, 1),
            new PlottableValue<SensorRawFeatureSet>("Quaternion W", v => "Quaternion W: " + v.ToString("0.0"), s => s.QuaternionW, 0, -1, 1),
                  
            new PlottableValue<SensorRawFeatureSet>("Rotation Pitch", v => "Rotation Pitch: " + (v / Math.PI * 180).ToString("0.0") + "°", s => s.RotationPitch, 0, -Math.PI, Math.PI),
            new PlottableValue<SensorRawFeatureSet>("Rotation Roll", v => "Rotation Roll: " + (v / Math.PI * 180).ToString("0.0") + "°", s => s.RotationRoll, 0, -Math.PI, Math.PI),
            new PlottableValue<SensorRawFeatureSet>("Rotation Yaw", v => "Rotation Yaw: " + (v / Math.PI * 180).ToString("0.0") + "°", s => s.RotationYaw, 0, -Math.PI, Math.PI),
                            
            new PlottableValue<SensorRawFeatureSet>("Rotation Rate X", v => "Rotation Rate X: " + v.ToString("0.0"), s => s.RotationRateX, 0, -5, 5),
            new PlottableValue<SensorRawFeatureSet>("Rotation Rate Y", v => "Rotation Rate Y: " + v.ToString("0.0"), s => s.RotationRateY, 0, -5, 5),
            new PlottableValue<SensorRawFeatureSet>("Rotation Rate Z", v => "Rotation Rate Z: " + v.ToString("0.0"), s => s.RotationRateZ, 0, -5, 5),
                          
            new PlottableValue<SensorRawFeatureSet>("Raw Acceleration X", v => "Raw Acceleration X: " + v.ToString("0.0"), s => s.RawAccelerationX, 0, -50, 50),
            new PlottableValue<SensorRawFeatureSet>("Raw Acceleration Y", v => "Raw Acceleration Y: " + v.ToString("0.0"), s => s.RawAccelerationY, 0, -50, 50),
            new PlottableValue<SensorRawFeatureSet>("Raw Acceleration Z", v => "Raw Acceleration Z: " + v.ToString("0.0"), s => s.RawAccelerationZ, 0, -50, 50),
                           
            new PlottableValue<SensorRawFeatureSet>("Linear Acceleration X", v => "Linear Acceleration X: " + v.ToString("0.0"), s => s.LinearAccelerationX, 0, -50, 50),
            new PlottableValue<SensorRawFeatureSet>("Linear Acceleration Y", v => "Linear Acceleration Y: " + v.ToString("0.0"), s => s.LinearAccelerationY, 0, -50, 50),
            new PlottableValue<SensorRawFeatureSet>("Linear Acceleration Z", v => "Linear Acceleration Z: " + v.ToString("0.0"), s => s.LinearAccelerationZ, 0, -50, 50),
                           
            new PlottableValue<SensorRawFeatureSet>("Gravity X", v => "Gravity X: " + v.ToString("0.0"), s => s.GravityX, 0, -5, 5),
            new PlottableValue<SensorRawFeatureSet>("Gravity Y", v => "Gravity Y: " + v.ToString("0.0"), s => s.GravityY, 0, -5, 5),
            new PlottableValue<SensorRawFeatureSet>("Gravity Z", v => "Gravity Z: " + v.ToString("0.0"), s => s.GravityZ, 0, -5, 5),
                     
            new PlottableValue<SensorRawFeatureSet>("Magnetic Heading", v => "Magnetic Heading: " + v.ToString("0.0"), s => s.MagneticHeading, 0, 0, 100),
            new PlottableValue<SensorRawFeatureSet>("True Heading", v => "True Heading: " + v.ToString("0.0"), s => s.TrueHeading, 0, 0, 100),
            new PlottableValue<SensorRawFeatureSet>("Heading Accuracy", v => "Heading Accuracy: " + v.ToString("0.0"), s => s.HeadingAccuracy, 0, 0, 100),
                   
            new PlottableValue<SensorRawFeatureSet>("Raw Magnetometer Reading X", v => "Magnetometer X: " + v.ToString("0.0"), s => s.MagnetometerX, 0, 0, 100),
            new PlottableValue<SensorRawFeatureSet>("Raw Magnetometer Reading Y", v => "Magnetometer Y: " + v.ToString("0.0"), s => s.MagnetometerY, 0, 0, 100),
            new PlottableValue<SensorRawFeatureSet>("Raw Magnetometer Reading Z", v => "Magnetometer Z: " + v.ToString("0.0"), s => s.MagnetometerZ, 0, 0, 100),
                        
            new PlottableValue<SensorRawFeatureSet>("Validity of Magnetometer Data", v => "Magnetometer Data is " + (v == 1 ? "valid" : "invalid!"), s => s.MagnetometerDataValid ? 1 : 0, 0, -1, 2)
        };

        private static PlottableValue<SensorFeatureSet>[] sensorFeatureValues = new PlottableValue<SensorFeatureSet>[]{
            new PlottableValue<SensorFeatureSet>("Rotation X (Pitch) Mean", v => "Rotation X Mean: " + (v / Math.PI * 180).ToString("0.0"), s => s.RotationXMean, 0, -Math.PI, Math.PI),
            new PlottableValue<SensorFeatureSet>("Rotation Y (Roll) Mean", v => "Rotation Y Mean: " + (v / Math.PI * 180).ToString("0.0"), s => s.RotationYMean, 0, -Math.PI, Math.PI),
            new PlottableValue<SensorFeatureSet>("Rotation Z (Yaw) Mean", v => "Rotation Z Mean: " + (v / Math.PI * 180).ToString("0.0"), s => s.RotationZMean, 0, -Math.PI, Math.PI),
            
            new PlottableValue<SensorFeatureSet>("Rotation X (Pitch) Std Deviation", v => "Rotation X StdDev: " + v.ToString("0.0"), s => s.RotationXStdDev, 0, -1, 1),
            new PlottableValue<SensorFeatureSet>("Rotation Y (Roll) Std Deviation", v => "Rotation Y StdDev: " + v.ToString("0.0"), s => s.RotationYStdDev, 0, -1, 1),
            new PlottableValue<SensorFeatureSet>("Rotation Z (Yaw) Std Deviation", v => "Rotation Z StdDev: " + v.ToString("0.0"), s => s.RotationZStdDev, 0, -1, 1),

            new PlottableValue<SensorFeatureSet>("Rotation X-Y (Pitch-Roll) Correlation", v => "Rotation X-Y Correlation: " + v.ToString("0.0"), s => s.RotationCorrelationXY, 0, -1, 1),
            new PlottableValue<SensorFeatureSet>("Rotation X-Z (Pitch-Yaw) Correlation", v => "Rotation X-Z Correlation: " + v.ToString("0.0"), s => s.RotationCorrelationXZ, 0, -1, 1),
            new PlottableValue<SensorFeatureSet>("Rotation Y-Z (Roll-Yaw) Correlation", v => "Rotation Y-Z Correlation: " + v.ToString("0.0"), s => s.RotationCorrelationYZ, 0, -1, 1),

            new PlottableValue<SensorFeatureSet>("Rotation X (Pitch) Energy", v => "Rotation X Energy: " + v.ToString("0.00"), s => s.RotationXEnergy, 0, -1, 1),
            new PlottableValue<SensorFeatureSet>("Rotation Y (Roll) Energy", v => "Rotation Y Energy: " + v.ToString("0.00"), s => s.RotationYEnergy, 0, -1, 1),
            new PlottableValue<SensorFeatureSet>("Rotation Z (Yaw) Energy", v => "Rotation Z Energy: " + v.ToString("0.00"), s => s.RotationZEnergy, 0, -1, 1),

            new PlottableValue<SensorFeatureSet>("Heading", v => "Phone Heading: " + (v / Math.PI * 180).ToString("0.0") + "°", s => s.Heading, 0, -Math.PI, Math.PI),
            new PlottableValue<SensorFeatureSet>("Heading Delta", v => "Heading Delta: " + (v / Math.PI * 180).ToString("0.0") + "°", s => s.HeadingDelta, 0, -Math.PI, Math.PI)
        };

        private static PlottableValue<KinectFeatureSet>[] kinectFeatureValues = new PlottableValue<KinectFeatureSet>[]{
            new PlottableValue<KinectFeatureSet>("Kinect: Shoulder Angle", v => "Shoulder Heading: " + (v / Math.PI * 180).ToString("0.0") + "°", s => s.ShoulderOrientation, 0, -Math.PI, Math.PI),
            new PlottableValue<KinectFeatureSet>("Kinect: Left Leg Angle", v => "Left Leg Angle: " + (v / Math.PI * 180) + "°", s => s.LeftLegToTorsoAngle, Math.PI, 0, 2 * Math.PI),
            new PlottableValue<KinectFeatureSet>("Kinect: Right Leg Angle", v => "Right Leg Angle: " + (v / Math.PI * 180) + "°", s => s.RightLegToTorsoAngle, Math.PI, 0, 2 * Math.PI)
        };

        private static PlottableValue<LearnerPredictedFeatureSet>[] learnerFeatureValues = new PlottableValue<LearnerPredictedFeatureSet>[]{
            new PlottableValue<LearnerPredictedFeatureSet>("Learner: Learned Status", v => "Learned: " + (v == 1d ? "true" : "false"), s => s.IsLearned ? 1d : 0d, 0, -0.5, 1.5),
            new PlottableValue<LearnerPredictedFeatureSet>("Learner: Left Leg Angle", v => "Predicted Left Leg Angle: " + (v / Math.PI * 180) + "°", s => s.PredictedLeftLegAngle, Math.PI, 0, 2 * Math.PI),
            new PlottableValue<LearnerPredictedFeatureSet>("Learner: Right Leg Angle", v => "Predicted Right Leg Angle: " + (v / Math.PI * 180) + "°", s => s.PredictedRightLegAngle, Math.PI, 0, 2 * Math.PI),
            new PlottableValue<LearnerPredictedFeatureSet>("Learner: Shoulder Angle", v => "Predicted Shoulder Angle: " + (v / Math.PI * 180) + "°", s => s.PredictedShoulderAngle, 0, -Math.PI, Math.PI)
        };

        private static PlottableValueGroup[] sensorRawValueGroups = new PlottableValueGroup[] {
            new PlottableValueGroup("Quaternion X, Y, Z without W", false, new int[]{ 0, 1, 2 }),
            new PlottableValueGroup("Rotation Pitch, Roll, Yaw", false, new int[]{ 4, 5, 6 }),
            new PlottableValueGroup("Rotation Rate X, Y, Z", false, new int[]{ 7, 8, 9 }),
            new PlottableValueGroup("Raw Acceleration X, Y, Z", false, new int[]{ 10, 11, 12 }),
            new PlottableValueGroup("Linear Acceleration X, Y, Z", false, new int[]{ 13, 14, 15 }),
            new PlottableValueGroup("Gravity X, Y, Z", false, new int[]{ 16, 17, 18 }),
            new PlottableValueGroup("Magnetic Heading Infos", false, new int[]{ 19, 20, 21 }),
            new PlottableValueGroup("Raw Magnetometer Readings", false, new int[]{ 22, 23, 24 })
        };

        private static PlottableValueGroup[] sensorFeatureValueGroups = new PlottableValueGroup[] {
            new PlottableValueGroup("Rotation Means", false, new int[]{ 0, 1, 2 }),
            new PlottableValueGroup("Rotation Std Deviations", false, new int[]{ 3, 4, 5 }),
            new PlottableValueGroup("Rotation Correlations", false, new int[]{ 6, 7, 8 }),
            new PlottableValueGroup("Rotation Energies", false, new int[]{ 9, 10, 11 })
        };

        private static PlottableValueGroup[] kinectFeatureValueGroups = new PlottableValueGroup[] {
            new PlottableValueGroup("Body Features", false, new int[]{ 0, 1, 2 })
        };

        private static PlottableValueGroup[] learnerFeatureValueGroups = new PlottableValueGroup[] {
            new PlottableValueGroup("Learner Features", false, new int[]{ 0, 1, 2 })
        };



        /// <summary>
        /// Returns a complete metadata package for a given type of feature set. 
        /// This is needed for the PlotterGroup control. It contains the following primary view-related data:
        ///  - A list (array) of PlottableValue objects, that is used to fill all 
        ///    three comboboxes for user-selected features. Each object also includes
        ///    a mapping to extract the selected value from a featureset
        ///    and plot it to the diagram.
        ///  - A list (array) of PlottableValueGroup objects, that is used to fill the first
        ///    combobox of 'feature groups', that is a convenient way to switch all three 
        ///    diagrams to show three related features at once (e.g. three axis values of a 
        ///    single 3d-sensor). Therefore, it includes a mapping to the indices of those
        ///    respective PlottableValue objects.
        ///  - The index of the PlottableValueGroup that is selected per default.
        ///  - The indices of PlottableValues that are selected per default. The default 
        ///    PlottableValueGroup is set to the "custom" value in this case.
        /// Those two lists and the default index are all returned at once as a tuple.
        /// </summary>
        /// <typeparam name="T">One of the following Types: CombinedFeatureSet, SensorComparisonFeatureSet</typeparam>
        /// <returns>A tuple containing the two lists described above and the default group index.</returns>
        public static Tuple<PlottableValue<T>[], PlottableValueGroup[], int, int[]> GetValueMapping<T>()
        {
            if (typeof(T) == typeof(CombinedFeatureSet))
            {
                return new Tuple<PlottableValue<T>[], PlottableValueGroup[], int, int[]>(
                    new PlottableValue<CombinedFeatureSet>[0]
                        .AddValues(sensorRawValues, "Sensor: ", x => x.RawSensors)
                        .AddValues(sensorFeatureValues, "Sensor Features: ", x => x.SensorFeatures)
                        .AddValues(kinectFeatureValues, "Kinect: ", x => x.KinectFeatures)
                        .AddValues(learnerFeatureValues, "Kinect: ", x => x.LearnerFeatures)
                        .Cast<PlottableValue<T>>().ToArray(),
                    BuildValueGroupList(
                        new Tuple<string, PlottableValueGroup[], int>("Sensors: ", sensorRawValueGroups, sensorRawValues.Length),
                        new Tuple<string, PlottableValueGroup[], int>("Sensor Features: ", sensorFeatureValueGroups, sensorFeatureValues.Length),
                        new Tuple<string, PlottableValueGroup[], int>("Kinect: ", kinectFeatureValueGroups, kinectFeatureValues.Length),
                        new Tuple<string, PlottableValueGroup[], int>("Learner Features: ", learnerFeatureValueGroups, learnerFeatureValues.Length)
                    ),
                    0,
                    new int[] { 38, 40, 39 }
                );

                // old default value presets:
                // { 38, 40, 39 } (Phone Heading, Shoulder Heading, Heading Delta)
                // { 38, 1, 2 }   (Phone Heading, Quaternion Y, Z)
                // { 42, 43, 45 } (Kinect right leg, learner status, right leg learned angle)
                // { 4, 35, 5 }   (Rotation Pitch, Pitch energy, Rotation Roll)
                // { 4, 5, 6 }    (Rotation Pitch, Rotation Roll, Rotation Yaw)
                // { 7, 8, 9 }    (Rotation Rate X, Y, Z)
                // { 0, 1, 2 }    (Rotation Quaternion X, Y, Z)
            }
            else if (typeof(T) == typeof(SensorComparisonFeatureSet))
            {
                return new Tuple<PlottableValue<T>[], PlottableValueGroup[], int, int[]>(
                    new PlottableValue<SensorComparisonFeatureSet>[0]
                        .AddValues(sensorRawValues, "Phone 1 Sensor: ", x => x.RawSensors1)
                        .AddValues(sensorRawValues, "Phone 2 Sensor: ", x => x.RawSensors2)
                        .Cast<PlottableValue<T>>().ToArray(),
                    BuildValueGroupList(
                        new Tuple<string, PlottableValueGroup[], int>("Phone 1 Sensors: ", sensorRawValueGroups, sensorRawValues.Length),
                        new Tuple<string, PlottableValueGroup[], int>("Phone 2 Sensors: ", sensorRawValueGroups, sensorRawValues.Length)),
                    1,
                    null
                );
            }
            else
            {
                return new Tuple<PlottableValue<T>[], PlottableValueGroup[], int, int[]>(new PlottableValue<T>[0], new PlottableValueGroup[0], 0, null);
            }
        }

        /// <summary>
        /// Private helper function to build more complex lists of PlottableValue objects from the basic lists, 
        /// declared as private static members of this class. For a more detailed explanation, see the description
        /// of the GetValueMapping method.
        /// The given list is appended to the given baselist. Every object appended also gets modified by
        /// transforming the value function to access the underlying object (accessor function) and optionally 
        /// prepending a prefix to the Name property.
        /// </summary>
        /// <typeparam name="T">The generic type parameter of the PlottableValue objects in the baselist.</typeparam>
        /// <typeparam name="TInner">The generic type parameter of the PlottableValue objects in the list to append.</typeparam>
        /// <param name="baselist">The base list, where the new list gets appended to.</param>
        /// <param name="list">The list to append.</param>
        /// <param name="prefix">A string prefix for the Name property of every PlottableValue 
        /// object in the list to be appended.</param>
        /// <param name="accessor">The accesor function that transforms the inner type
        /// (list to be appended) to the outer type (list to append to).</param>
        /// <returns>The merged list, i.e. the baselist with the transformed list appended to it.</returns>
        private static PlottableValue<T>[] AddValues<T, TInner>(this PlottableValue<T>[] baselist,
                            PlottableValue<TInner>[] list, string prefix, Func<T, TInner> accessor)
        {
            PlottableValue<T>[] newlist = new PlottableValue<T>[baselist.Length + list.Length];
            Array.Copy(baselist, newlist, baselist.Length);

            prefix = prefix ?? string.Empty;

            for (int i = 0; i < list.Length; i++)
            {
                PlottableValue<TInner> toTransform = list[i];

                var transformed = new PlottableValue<T>(prefix + toTransform.Name,
                                                        toTransform.Label,
                                                        x => toTransform.GetValue(accessor(x)),
                                                        toTransform.Default,
                                                        toTransform.Minimum,
                                                        toTransform.Maximum);
                newlist[i + baselist.Length] = transformed;
            }

            return newlist;
        }

        /// <summary>
        /// Builds a list of PlottableValueGroup objects by combining all given PlottableValueGroup lists (second tuple items).
        /// Each object in the given lists gets a string prefix prepended to the Name property (given as the first tuple item, each).
        /// The third tuple item has to be the number of list items in the corresponding PlottableValue list.
        /// Additionally, an item named 'Custom' is added to the end of the list, that is used to let the user
        /// select each of the three values to plot himself.
        /// </summary>
        /// <param name="groupLists">The list of 3-tuples, descibed above. Is used to build the new PlottableValueGroup list.</param>
        /// <returns>The merged PlottableValueGroup list.</returns>
        private static PlottableValueGroup[] BuildValueGroupList(params Tuple<string, PlottableValueGroup[], int>[] groupLists)
        {
            int total = 0; for (int i = 0; i < groupLists.Length; i++) total += groupLists[i].Item2.Length;
            var newGroupList = new PlottableValueGroup[total + 1];

            int c = 0, c2 = 0;
            for (int i = 0; i < groupLists.Length; i++)
            {
                var prefix = groupLists[i].Item1;
                for(int j = 0; j < groupLists[i].Item2.Length; j++){
                    var toTransform = groupLists[i].Item2[j];
                    newGroupList[c++] = new PlottableValueGroup(prefix + toTransform.Name, false, 
                                toTransform.SensorValueIndices.Select(x => x + c2).ToArray());
                }
                c2 += groupLists[i].Item3;
            }

            newGroupList[newGroupList.Length - 1] = new PlottableValueGroup("Custom...", true, null);

            return newGroupList;
        }
    }

    public class PlottableValue<T>
    {
        public string Name { get; set; }
        public Func<double, string> Label { get; set; }
        public Func<T, double> GetValue { get; set; }
        public double Default { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }

        public PlottableValue(string name, Func<double, string> label, Func<T, double> getvalue, double def, double min, double max)
        {
            Name = name;
            Label = label;
            GetValue = getvalue;
            Default = def;
            Minimum = min;
            Maximum = max;
        }
    }

    public class PlottableValueGroup
    {
        public string Name { get; set; }
        public bool Custom { get; set; }
        public int[] SensorValueIndices { get; set; }

        public PlottableValueGroup(string name, bool custom, int[] indexes)
        {
            Name = name;
            Custom = custom;
            SensorValueIndices = indexes;
        }
    }
}
