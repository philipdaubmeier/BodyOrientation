using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BodyOrientationLib;

namespace BodyOrientationGUI
{
    public partial class SensorComparisonWindow : Window
    {
        public SensorComparisonWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) => { StartSources(true); };
        }

        private void StartSources(bool play)
        {
            var multiplexer = new SensorComparisonMultiplexer();

            // TODO: path is hardcoded -> use some file selection dialog
            var recorder = new BinaryRecorder<SensorComparisonFeatureSet>(@"C:\sensorcomparisonsavedata.scd", play ? RecorderMode.PlayRealtime : RecorderMode.Record);
            this.Closed += (s, e) => { recorder.Dispose(); };

            if (play)
            {
                // Playing mode: Just read the combined recording, extract all three raw-value-set objects
                // and put them back into the multiplexer (to reprocess the sequence)
                recorderControls.BindToRecorder<SensorComparisonFeatureSet>(recorder, plotterGroup);
                recorder.FrameRead += (s, e) =>
                {
                    multiplexer.PushRawSensor2Values(e.Frame.RawSensors2);
                    
                    // Push first multiplex stream item at the end (this triggers the multiplexing)
                    multiplexer.PushRawSensor1Values(e.Frame.RawSensors1);
                };

                // Dispose the recorder input stream when application is closed
                this.Closed += (s, e) => { recorder.Dispose(); };
            }
            else
            {
                // Live mode: Set up the two phone sensor sources (the individual sources, in turn, can be either 
                // recordings or live streams) and forward them into the multiplexer
                var sensorSource1 = new SensorRawFeatureSource(AbstractRawFeatureSource<SensorRawFeatureSet>.Mode.UseLiveStream, 4001);
                sensorSource1.NewItem += (s, e) => { multiplexer.PushRawSensor1Values(e.Item); };
                listBoxStatusUpdates.Items.Add("Listening on tcp port 4001 for phone #1.");
                sensorSource1.ExceptionOccured += (s, e) => { listBoxStatusUpdates.Items.Add(string.Format("{0}: \"{1}\" {2}", DateTime.Now.ToShortTimeString(), e.Exception.Message, e.Context.ToString())); };

                var sensorSource2 = new SensorRawFeatureSource(AbstractRawFeatureSource<SensorRawFeatureSet>.Mode.UseLiveStream, 4002);
                sensorSource2.NewItem += (s, e) => { multiplexer.PushRawSensor2Values(e.Item); };
                listBoxStatusUpdates.Items.Add("Listening on tcp port 4002 for phone #2.");
                sensorSource2.ExceptionOccured += (s, e) => { listBoxStatusUpdates.Items.Add(string.Format("{0}: \"{1}\" {2}", DateTime.Now.ToShortTimeString(), e.Exception.Message, e.Context.ToString())); };

                // Dispose all input streams when application is closed
                this.Closed += (s, e) => { sensorSource1.Dispose(); sensorSource2.Dispose(); };
            }
            
            // A new object was multiplexed from all input values, update all GUI components accordingly
            multiplexer.ItemMultiplexed += (s, e) => {
                plotterGroup.Plot(e.MultiplexedItem);

                phoneModel1.Update3dPhoneModel(e.MultiplexedItem.RawSensors1);
                phoneModel2.Update3dPhoneModel(e.MultiplexedItem.RawSensors2);
                
                // Record the frame, if we are not playing a recording
                if (!play) recorder.RecordFrame(e.MultiplexedItem);
            };
        }
    }
}
