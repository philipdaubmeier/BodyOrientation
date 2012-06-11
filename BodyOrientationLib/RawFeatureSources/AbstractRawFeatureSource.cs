using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace BodyOrientationLib
{
    public abstract class AbstractRawFeatureSource<T> : IDisposable
        where T : IMultiplexable, IRecordable, new()
    {
        public enum Mode
        {
            UseLiveStream,
            UseLiveStreamAndRecordIt,
            PlayRecording
        }

        public event EventHandler<NewItemEventArgs<T>> NewItem;

        protected abstract string defaultRecordingPath { get; }

        protected Mode sourceMode;
        protected string recordingPath = string.Empty;
        protected BinaryRecorder<T> recorder = null;


        public AbstractRawFeatureSource(Mode sourceMode) : this(sourceMode, null) { }

        public AbstractRawFeatureSource(Mode sourceMode, string recordingPath) : this(sourceMode, null, null) { }

        public AbstractRawFeatureSource(Mode sourceMode, string recordingPath, object constructorData)
        {
            Initialize(constructorData);

            this.sourceMode = sourceMode;
            this.recordingPath = recordingPath ?? defaultRecordingPath;

            if (sourceMode == Mode.PlayRecording)
                StartRecordedStream();
            else
                StartLiveStream();
        }

        protected abstract void Initialize(object constructorData);

        protected void StartRecordedStream()
        {
            recorder = new BinaryRecorder<T>(recordingPath, RecorderMode.PlayRealtime);
            recorder.FrameRead += new EventHandler<FrameReadEventArgs<T>>(RecorderFrameRead);
            recorder.Play();
        }

        protected void RecorderFrameRead(object sender, FrameReadEventArgs<T> e)
        {
            OnNewItem(e.Frame);
        }

        protected abstract void StartLiveStream();

        protected void OnNewItem(T item)
        {
            if (NewItem != null && Application.Current != null)
                Application.Current.Dispatcher.Invoke(NewItem, this, new NewItemEventArgs<T>() { Item = item });
        }

        public void Dispose()
        {
            if (recorder != null)
                recorder.Close();
        }
    }

    public class NewItemEventArgs<T> : EventArgs
        where T : IMultiplexable, IRecordable, new()
    {
        public T Item { get; set; }
    }
}
