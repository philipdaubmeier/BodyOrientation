using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BodyOrientationLib;
using Microsoft.Research.Kinect.Nui;
using System.Windows.Media.Media3D;
using Microsoft.Win32;

namespace BodyOrientationGUI
{
    public partial class MainWindow : Window
    {
        private BinaryRecorder<CombinedFeatureSet> recorder;
        private bool play;

        private volatile bool loaded = false;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) => { loaded = true; };
        }

        #region GUI elements for "Open a recording to play", or "chose a file to record to"
        private void MenuItemOpenRecording_Clicked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;

            var dialog = new OpenFileDialog();
            SetFileDialogProperties(dialog);
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            
            // Play selected file
            if (dialog.ShowDialog().GetValueOrDefault(false))
                StartSources(true, dialog.FileName);
        }

        private void MenuItemStartRecording_Clicked(object sender, RoutedEventArgs e)
        {
            if (!loaded)
                return;

            var dialog = new SaveFileDialog();
            SetFileDialogProperties(dialog);
            
            // Record into selected file (may get newly created first)
            if (dialog.ShowDialog().GetValueOrDefault(false))
                StartSources(false, dialog.FileName);
        }

        private void SetFileDialogProperties(FileDialog dialog)
        {
            dialog.FileName = "BodyOrientation Savedata";
            dialog.AddExtension = true;
            dialog.DefaultExt = ".csd";
            dialog.Filter = "Combined save data (*csd)|*.csd";
            dialog.FilterIndex = 0;
        }
        #endregion

        private bool recordRda = false;
        private RdaExporter<CombinedFeatureSet> rdaExporter;
        private void RecordClicked(object sender, RoutedEventArgs e)
        {
            recordRda = !recordRda;
        }

        private void StartSources(bool play, string path)
        {
            // Temporary quick and dirty solution: Only let the user chose once to play or 
            // record a file, then the app has to get restarted
            // TODO: make this more robust, and let the user load and record more than once
            MenuItemOpenRecording.IsEnabled = false;
            MenuItemStartRecording.IsEnabled = false;

            this.play = play;
            
            var multiplexer = new CombinedMultiplexer();

            // Obsolete: ARFF (Attribute-Relation File Format) exporter for the Weka data mining software
            //var arffExporter = new ArffExporter<SensorFeatureSet>(@"C:\arffexport.arff", new Dictionary<string, string[]>() { { "Class", Posture.EnumerateStateNames().ToArray() } });
            //this.Closed += (s, e) => { arffExporter.Dispose(); };

            rdaExporter = new RdaExporter<CombinedFeatureSet>(System.IO.Path.Combine(Environment.GetFolderPath(
                                                              System.Environment.SpecialFolder.MyDocuments), @"bo.txt"));
            this.Closed += (s, e) => { rdaExporter.Dispose(); };

            recorder = new BinaryRecorder<CombinedFeatureSet>(path, play ? RecorderMode.PlayRealtime : RecorderMode.Record);
            this.Closed += (s, e) => {recorder.Dispose(); };

            if (play)
            {
                Quaternion calibQuat = new Quaternion();

                // TODO: these lines are just for testing, remove at some point
                double calibAngle = 0d;
                this.phoneModel.CalibrationEnabled = true;
                this.phoneModel.Calibrated += (s, e) => { calibQuat = e.CalibrationQuaternion; };
                multiplexer.CalibrationAngleCalculated += (s, e) => { calibAngle = e.CalibrationAngle; };

                // Playing mode: Just read the combined recording, extract all three raw-value-set objects
                // and put them back into the multiplexer (to reprocess the sequence)
                recorderControls.BindToRecorder<CombinedFeatureSet>(recorder, plotterGroup);
                recorder.FrameRead += (s, e) =>
                {
                    // TODO: uncomment the following lines in final version, this is just commented out for testing
                    // Read the stored calibration data and apply to the phone model view
                    //if (e.Frame.RawManual.CalibrationQuaternion != calibQuat)
                    //{
                    //    calibQuat = e.Frame.RawManual.CalibrationQuaternion;
                    //    this.phoneModel.CalibrateManually(calibQuat);
                    //}

                    // TODO: just for testing, delete next 2 lines afterwards
                    e.Frame.RawManual.CalibrationQuaternion = calibQuat;
                    e.Frame.RawManual.CalibrationAngle = calibAngle;

                    multiplexer.PushRawKinectValues(e.Frame.RawKinect);
                    multiplexer.PushRawManualValues(e.Frame.RawManual);

                    // Push first multiplex stream item at the end (this triggers the multiplexing)
                    multiplexer.PushRawSensorValues(e.Frame.RawSensors);
                };

                // Dispose the recorder input stream when application is closed
                this.Closed += (s, e) => { recorder.Dispose(); };
            }
            else
            {
                // Live mode: Set up the three sources (the individual sources, in turn, can be either 
                // recordings or live streams) and forward them into the multiplexer
                KinectRawFeatureSource kinectSource = null;
                try
                {
                    kinectSource = new KinectRawFeatureSource(AbstractRawFeatureSource<KinectRawFeatureSet>.Mode.UseLiveStream);
                    kinectSource.NewItem += (s, e) => { multiplexer.PushRawKinectValues(e.Item); };
                }
                catch { }

                var sensorSource = new SensorRawFeatureSource(AbstractRawFeatureSource<SensorRawFeatureSet>.Mode.UseLiveStream, 3547);
                sensorSource.NewItem += (s, e) => { multiplexer.PushRawSensorValues(e.Item); };
                sensorSource.ExceptionOccured += (s, e) => { listBoxStatusUpdates.Items.Add(string.Format("{0}: \"{1}\" {2}", DateTime.Now.ToShortTimeString(), e.Exception.Message, e.Context.ToString())); };

                var manualSource = new ManualRawFeatureSource(AbstractRawFeatureSource<ManualRawFeatureSet>.Mode.UseLiveStream);
                manualSource.NewItem += (s, e) => { multiplexer.PushRawManualValues(e.Item); };
                buttonNextStep.Click += (s, e) => { manualSource.NextPhase(); };

                // React to changing calibration of the phone
                this.phoneModel.CalibrationEnabled = true;
                this.phoneModel.Calibrated += (s, e) => { manualSource.SetNewCalibration(e.CalibrationQuaternion); };
                multiplexer.CalibrationAngleCalculated += (s, e) => { manualSource.SetNewCalibrationAngle(e.CalibrationAngle); };

                // Dispose all input streams when application is closed
                this.Closed += (s, e) => { kinectSource.Dispose(); sensorSource.Dispose(); manualSource.Dispose(); };
            }
            
            // A new object was multiplexed from all input values, update all GUI components accordingly
            multiplexer.ItemMultiplexed += new EventHandler<ItemMultiplexedEventArgs<CombinedFeatureSet>>(multiplexer_ItemMultiplexed);
        }

        private void multiplexer_ItemMultiplexed(object sender, ItemMultiplexedEventArgs<CombinedFeatureSet> e)
        {
            skeletonModel.UpdateSkeleton(e.MultiplexedItem.RawKinect.SkeletonJoints);
            predictedSkeletonModel.UpdateSkeleton(PredictSkeleton(e.MultiplexedItem.LearnerFeatures));

            labelCurrentPhase.Text = e.MultiplexedItem.RawManual.BodyPosture.ToString();
            labelNextPhase.Text = e.MultiplexedItem.RawManual.NextPosture.ToString();

            plotterGroup.Plot(e.MultiplexedItem);

            phoneModel.Update3dPhoneModel(e.MultiplexedItem.RawSensors);

            if (recordRda)
                rdaExporter.WriteData(e.MultiplexedItem);

            // Here we can export different value sets to ARFF
            //arffExporter.WriteData(e.MultiplexedItem.SensorFeatures);

            // Record the frame, if we are not playing a recording
            if (!play) recorder.RecordFrame(e.MultiplexedItem);
        }

        private class PredictedLeg
        {
            public Vector3D Knee { get; private set; }
            public Vector3D Ankle { get; private set; }
            public Vector3D Foot { get; private set; }

            public PredictedLeg(bool right, double legAngle, Vector3D torsoNormal, Vector3D hip, Vector3D otherHip, Vector3D knee, Vector3D ankle, Vector3D foot)
            {
                var rotationMatrix = Matrix3D.Identity;

                var tigh = knee - hip;
                var shank = ankle - knee;
                var toes = foot - ankle;
                double tighLength = tigh.Length;

                // The rotation axis is the intersection of the torso plane and the righthip-lefthip-knee plane.
                // This means the rotation axis is perpendicular to both plane normals and 
                // is therefore calculated via the cross product of both normals.
                var hipHipKneePlaneNormal = PlaneNormal(hip, otherHip, knee).NormalizeVector();
                var rotationAxis = Vector3D.CrossProduct(hipHipKneePlaneNormal, torsoNormal);

                // Old, wrong rotation axis
                //var rotationAxis = PerpendicularInsidePlane(tigh, torsoNormal);
                if(right)
                    rotationMatrix.Rotate(new Quaternion(rotationAxis, 270 - legAngle.ToDegrees()));
                else
                    rotationMatrix.Rotate(new Quaternion(rotationAxis, 90 + legAngle.ToDegrees()));

                // Calculate new tigh by rotating the Torso normal vector and scaling it to the tigh length
                var newTigh = torsoNormal * rotationMatrix * tighLength;

                this.Knee = hip + newTigh;
                this.Ankle = this.Knee + shank;
                this.Foot = this.Ankle + toes;
            }

            /// <summary>
            /// Returns the normal vector of a plane that is described by three points
            /// </summary>
            private Vector3D PlaneNormal(Vector3D p1, Vector3D p2, Vector3D p3)
            {
                return Vector3D.CrossProduct(p2 - p1, p3 - p1);
            }

            /// <summary>
            /// Given a line inside a plane and the normal vector of the same plane, this
            /// function returns the perpendicular vector to this line inside the plane.
            /// </summary>
            private Vector3D PerpendicularInsidePlane(Vector3D line, Vector3D planeNormal)
            {
                return Vector3D.CrossProduct(line, planeNormal);
            }
        }

        private IEnumerable<Joint> PredictSkeleton(LearnerPredictedFeatureSet learnedFeatures)
        {
            var skel = SkeletonExtensions.ExampleSkeleton;

            var vectors = skel.ToDictionary(x => x.ID, x => x.Position.ToVector3D());

            var torsoPlaneNormal = PlaneNormal(vectors[JointID.ShoulderCenter], vectors[JointID.HipLeft], vectors[JointID.HipRight]).NormalizeVector();

            var leftLeg = new PredictedLeg(false, learnedFeatures.PredictedLeftLegAngle, torsoPlaneNormal,
                            vectors[JointID.HipLeft], vectors[JointID.HipRight], vectors[JointID.KneeLeft], vectors[JointID.AnkleLeft], vectors[JointID.FootLeft]);
            var rightLeg = new PredictedLeg(true, learnedFeatures.PredictedRightLegAngle, torsoPlaneNormal,
                            vectors[JointID.HipRight], vectors[JointID.HipLeft], vectors[JointID.KneeRight], vectors[JointID.AnkleRight], vectors[JointID.FootRight]);

            var offset = Math.Min(rightLeg.Foot.Y, leftLeg.Foot.Y) - vectors[JointID.FootRight].Y;

            var shoulderRotation = Matrix3D.Identity;
            shoulderRotation.RotateAt(new Quaternion(new Vector3D(0, 1, 0), learnedFeatures.PredictedShoulderAngle.ToDegrees()), 
                                        (Point3D)vectors[JointID.Spine]);

            for (int i = 0; i < (int)JointID.Count; i++)
            {
                Joint joint = skel[i];
                var vec = joint.Position;

                Vector3D newVec = new Vector3D();
                switch (skel[i].ID)
                {
                    case JointID.KneeRight: { newVec = rightLeg.Knee; break; }
                    case JointID.AnkleRight: { newVec = rightLeg.Ankle; break; }
                    case JointID.FootRight: { newVec = rightLeg.Foot; break; }
                    case JointID.KneeLeft: { newVec = leftLeg.Knee; break; }
                    case JointID.AnkleLeft: { newVec = leftLeg.Ankle; break; }
                    case JointID.FootLeft: { newVec = leftLeg.Foot; break; }
                    default: { newVec = vec.ToVector3D(); break; }
                }

                newVec = (Vector3D)(((Point3D)newVec) * shoulderRotation);
                newVec.Y -= offset;

                joint.Position = newVec.ToNuiVector();
                skel[i] = joint;
            }

            return skel;
        }

        /// <summary>
        /// Returns the normal vector of a plane that is described by three points
        /// </summary>
        private Vector3D PlaneNormal(Vector3D p1, Vector3D p2, Vector3D p3)
        {
            return Vector3D.CrossProduct(p2 - p1, p3 - p1);
        }
    }
}
