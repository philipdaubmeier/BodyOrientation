using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Kinect.Nui;

namespace BodyOrientationLib
{
    public class KinectRawFeatureSource : AbstractRawFeatureSource<KinectRawFeatureSet>
    {
        protected override string defaultRecordingPath { get { return @"C:\kinectsavedata.ksd"; } }


        public KinectRawFeatureSource(Mode sourceMode) : this(sourceMode, null) { }

        public KinectRawFeatureSource(Mode sourceMode, string recordingPath) : base(sourceMode, recordingPath) { }

        protected override void Initialize(object constructorData) { }


        protected override void StartLiveStream()
        {
            Runtime kinectDevice;
            try
            {
                if (Runtime.Kinects.Count > 0)
                {
                    kinectDevice = Runtime.Kinects[0];
                    kinectDevice.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);
                }
                else
                    throw new InvalidOperationException();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("No kinect connected!", ex);
            }

            ////Must set 'TransformSmooth' to true (set just after call to Initialize)
            ////Use to transform and reduce jitter
            //kinectDevice.SkeletonEngine.TransformSmooth = true;
            //var parameters = new TransformSmoothParameters
            //{
            //    Smoothing = 0.75f,
            //    Correction = 0.0f,
            //    Prediction = 0.0f,
            //    JitterRadius = 0.05f,
            //    MaxDeviationRadius = 0.04f
            //};
            //kinectDevice.SkeletonEngine.SmoothParameters = parameters;

            if(sourceMode == Mode.UseLiveStreamAndRecordIt)
                recorder = new BinaryRecorder<KinectRawFeatureSet>(recordingPath, RecorderMode.Record);

            kinectDevice.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(KinectSkeletonFrameReady);

            //kinectDevice.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
        }

        private void KinectSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            var skeleton = e.SkeletonFrame.Skeletons.FirstOrDefault(x => x.TrackingState == SkeletonTrackingState.Tracked);
            if (skeleton != null)
            {
                var kinectRecording = new KinectRawFeatureSet(skeleton.Joints);

                if (sourceMode == Mode.UseLiveStreamAndRecordIt)
                    recorder.RecordFrame(kinectRecording);

                OnNewItem(kinectRecording);
            }
        }
    }
}
