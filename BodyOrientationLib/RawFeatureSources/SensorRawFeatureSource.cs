using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BodyOrientationLib
{
    public class SensorRawFeatureSource : AbstractRawFeatureSource<SensorRawFeatureSet>
    {
        protected override string defaultRecordingPath { get { return @"C:\sensorsavedata.ssd"; } }

        protected SensorServer<SensorRawFeatureSet> server = null;
        protected int tcpPort = -1;

        public event EventHandler<ExceptionOccuredEventArgs> ExceptionOccured;

        public SensorRawFeatureSource(Mode sourceMode) : this(sourceMode, -1) { }

        public SensorRawFeatureSource(Mode sourceMode, int tcpListenPort) : this(sourceMode, null, tcpListenPort) { }

        public SensorRawFeatureSource(Mode sourceMode, string recordingPath, int tcpListenPort) : base(sourceMode, recordingPath, tcpListenPort) {}
        
        protected override void Initialize(object constructorData)
        {
            var tcpListenPort = (int)constructorData;
            if (tcpListenPort > 0)
                tcpPort = tcpListenPort;
        }


        protected override void StartLiveStream()
        {
            server = new SensorServer<SensorRawFeatureSet>();

            server.ExceptionOccured += this.ExceptionOccured;
            server.ValuesReceived += new EventHandler<ValuesReceivedEventArgs<SensorRawFeatureSet>>(ReceivedValuesFromPhone);

            if (tcpPort > 0)
                server.Start(tcpPort);
            else
                server.Start();
        }

        private void ReceivedValuesFromPhone(object sender, ValuesReceivedEventArgs<SensorRawFeatureSet> e)
        {
            if (sourceMode == Mode.UseLiveStreamAndRecordIt)
                recorder.RecordFrame(e.SensorReading);

            OnNewItem(e.SensorReading);
        }
    }
}
