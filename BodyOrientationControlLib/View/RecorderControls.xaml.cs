using System.Windows;
using System.Windows.Controls;
using BodyOrientationLib;
using System;

namespace BodyOrientationControlLib
{
    public partial class RecorderControls : UserControl
    {
        private RoutedEventHandler playAction = null;
        private RoutedEventHandler pauseAction = null;
        private RoutedPropertyChangedEventHandler<double> valueChangedAction = null;
        private Action unsubscribeRecorderFrameRead = null;

        public RecorderControls()
        {
            InitializeComponent();
        }

        public void BindToRecorder<T>(BinaryRecorder<T> recorder, PlotterGroupBase<T> plotterGroup) where T : IRecordable, new()
        {
            // In case it is already bound to some other recorder -> release it
            ReleaseRecorder();

            // Set the actions; the recorder and plotterGroup reference is 
            // remembered in the labda function context
            EventHandler<FrameReadEventArgs<T>> frameRead = (s, e) => { 
                progress.Value = e.FrameNumber; 
            };
            playAction = (s, e) => { recorder.Play(); };
            pauseAction = (s, e) => { recorder.Pause(); };
            valueChangedAction = (s, e) =>
            {
                if (recorder.Seek((long)e.NewValue))
                    plotterGroup.Clear();
            };

            // Bind all Events and values
            play.Click += playAction;
            pause.Click += pauseAction;
            progress.Value = 0;
            progress.Minimum = 0;
            progress.Maximum = recorder.RecordingLength;
            progress.SmallChange = recorder.RecordingLength / 20;
            progress.LargeChange = recorder.RecordingLength / 10;
            progress.ValueChanged += valueChangedAction;
            recorder.FrameRead += frameRead;
            unsubscribeRecorderFrameRead = () => { recorder.FrameRead -= frameRead; };
            
            // Enable buttons
            SetEnabled(true);
        }

        public void ReleaseRecorder()
        {
            bool released = false;
            if (playAction != null)
            {
                play.Click -= playAction;
                playAction = null;
                released = released || true;
            }
            if (pauseAction != null)
            {
                pause.Click -= pauseAction;
                pauseAction = null;
                released = released || true;
            }
            if (valueChangedAction != null)
            {
                progress.ValueChanged -= valueChangedAction;
                valueChangedAction = null;
                released = released || true;
            }
            if (unsubscribeRecorderFrameRead != null)
            {
                unsubscribeRecorderFrameRead();
                unsubscribeRecorderFrameRead = null;
                released = released || true;
            }

            if (released)
                SetEnabled(false);
        }

        private void SetEnabled(bool newvalue)
        {
            play.IsEnabled = newvalue;
            pause.IsEnabled = newvalue;
            progress.IsEnabled = newvalue;
        }
    }
}
