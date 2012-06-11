using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows;

namespace BodyOrientationLib
{
    public enum RecorderMode
    {
        Record,
        PlayRealtime,
        PlayFullspeed
    }

    public class FrameReadEventArgs<T> : EventArgs
    {
        public T Frame { get; set; }
        public long FrameNumber { get; set; }
    }

    /// <summary>
    /// Records or plays binary file stream recordings.
    /// 
    /// Author: Philip Daubmeier
    /// </summary>
    /// <typeparam name="T">The IRecordable that represents one frame in the recording 
    /// that should be written or played back.</typeparam>
    public class BinaryRecorder<T> : IDisposable where T: IRecordable, new()
    {
        private DateTime? timeElapsed = null;
        private Stream savefile = null;
        private FileInfo recordingFileInfo = null;
        private BinaryWriter filewriter = null;
        private BinaryReader filereader = null;
        private RecorderMode mode = RecorderMode.Record;

        private int sizeOfFrame = -1;
        private long framesRecorded = 0;

        private Thread _playingThread;
        private AutoResetEvent pauseEvent = new AutoResetEvent(false);
        private volatile bool _paused = false;
        private volatile bool _seeking = false;
        private long _currentFrame = 0;
        private long _seekOffset = 0;

        private const int notSeekingScope = 5;

        /// <summary>
        /// Returns the size of a single frame in the recording including the 
        /// overhead per frame of the recorder. The size of the frame itself is
        /// assumed to be constant over all frames and is specific to the type
        /// of IRecordable given in the generic type parameter for this recorder.
        /// </summary>
        private int SizeOfFrame
        {
            get
            {
                // Get the size, if not yet in cache
                if (sizeOfFrame < 0)
                {
                    // Measure the size of a frame by creating a empty recordable 
                    // and writing it to a temporary memory stream.
                    // IMPORTANT: it is assumed that every frame (especially the 
                    // empty frame) is exactly equally large
                    using (MemoryStream tmpMemStream = new MemoryStream())
                    using (BinaryWriter tmpBw = new BinaryWriter(tmpMemStream))
                    {
                        T measurementUnit = new T();
                        measurementUnit.WriteToRecorder(tmpBw);
                        sizeOfFrame = (int)Math.Min(tmpMemStream.Position, int.MaxValue);
                        sizeOfFrame += SizeOfRecordingOverhead;
                    }
                }

                // Return the cached size
                return sizeOfFrame;
            }
        }

        /// <summary>
        /// Returns the size of the recording overhead per frame.
        /// Currently, this is just the elapsed time (an Int32).
        /// </summary>
        private int SizeOfRecordingOverhead
        {
            get
            {
                return sizeof(Int32);
            }
        }

        /// <summary>
        /// Returns the length of the recording in frames. If it is in record mode, 
        /// this returns the recorded frames until now. In playing mode, it 
        /// returns the total number of frames in the currently playing file.
        /// </summary>
        public long RecordingLength
        {
            get
            {
                if (mode == RecorderMode.Record)
                    return framesRecorded;
                else
                    return recordingFileInfo.Length / SizeOfFrame;
            }
        }

        /// <summary>
        /// Creates a new instance of a Recorder with a given IRecordable as generic type parameter.
        /// Depending on the mode, either a new file is created, where frames can be recorded with
        /// calls to "RecordFrame" or a recorded file can be played via calling "Play" and subscribing
        /// to the "FrameRead" event.
        /// </summary>
        /// <param name="path">The full path to the file that should be created, or played.</param>
        /// <param name="mode">One of the following modes: Record, PlayRealtime or PlayFullspeed</param>
        public BinaryRecorder(string path, RecorderMode mode)
        {
            this.mode = mode;
            if (mode == RecorderMode.Record)
            {
                // Recorder mode -> create a file and a binary writer
                savefile = new FileStream(path, FileMode.Create);
                filewriter = new BinaryWriter(savefile);
            }
            else
            {
                // Playing mode -> try to read the file with a binary reader
                try
                {
                    recordingFileInfo = new FileInfo(path);
                    savefile = new FileStream(path, FileMode.Open);
                }
                catch (FileNotFoundException ex)
                {
                    throw new FileNotFoundException("Recordfile not found!", ex);
                }
                filereader = new BinaryReader(savefile);
            }
        }

        /// <summary>
        /// Writes the given frame into the file, together with a hint of 
        /// how much time has elapsed since the last recorded frame.
        /// </summary>
        public void RecordFrame(T frame)
        {
            // Write elapsed time and the frame itself into the stream.
            // The specific implementation is contained in every IRecordable.
            WriteElapsedMilliseconds(filewriter);
            frame.WriteToRecorder(filewriter);

            // Count recorded frames
            framesRecorded++;
        }

        /// <summary>
        /// Is only fired in playing mode, if a new frame has been read out of the file stream.
        /// </summary>
        public event EventHandler<FrameReadEventArgs<T>> FrameRead;

        /// <summary>
        /// Starts to play the given recording file, or resumes playing if paused.
        /// </summary>
        public void Play()
        {
            if (_playingThread != null && _playingThread.IsAlive)
            {
                // Signal the working thread to continue playing, if it was paused
                pauseEvent.Set();
                _paused = false;
            }
            else
            {
                // Initialize working thread for playing the recorded file
                _playingThread = new Thread(new ThreadStart(() =>
                {
                    Stopwatch sw = new Stopwatch();
                    while (true)
                    {
                        try
                        {
                            // If signaled to pause, wait for the AutoResetEvent to unpause it again
                            if(_paused)
                                pauseEvent.WaitOne();

                            // If signaled to seek to a given position, jump to the corresponding offset in the stream
                            if (_seeking)
                            {
                                ((FileStream)savefile).Seek(_seekOffset, SeekOrigin.Begin);
                                _currentFrame = _seekOffset / SizeOfFrame;
                                _seeking = false;

                                // Read the elapsed milliseconds and discard them.
                                // This just has to be done to advance the stream 
                                // by the offset of this timestamp overhead
                                ReadElapsedMilliseconds(filereader);

                                // Restart with a fresh clock to measure the time to the next frame
                                sw.Restart();
                            }
                            else
                            {
                                int sleep = ReadElapsedMilliseconds(filereader);

                                // TODO: investigate issue with recorded milliseconds. 
                                // Workaround until then: just set 'sleep' to hardcoded 60 ms
                                sleep = 60;

                                // If set to play in realtime (rather than full speed), wait a given 
                                // timespan between the frames. This timespan is given in the binary stream.
                                if (mode == RecorderMode.PlayRealtime)
                                {
                                    int sleepLeft = (int)Math.Min(Math.Max(0, sleep - sw.ElapsedMilliseconds), int.MaxValue);
                                    Thread.Sleep(sleepLeft);
                                    sw.Restart();
                                }

                                // Keep track of the currently played frame number
                                _currentFrame++;
                            }

                            // Create an empty frame and read the values into it.
                            T frame = new T();
                            frame.ReadFromRecorder(filereader);

                            // Fire back an event to the GUI Thread for the read frame
                            // together with the current frame number
                            if(FrameRead != null)
                                Application.Current.Dispatcher.Invoke(FrameRead, this, 
                                    new FrameReadEventArgs<T>() { Frame = frame, FrameNumber = _currentFrame });
                        }
                        catch (EndOfStreamException) { break; }
                    }
                }));
                _playingThread.Start();
            }
        }

        /// <summary>
        /// Pauses the playback of the recording.
        /// </summary>
        public void Pause()
        {
            // Signal the working thread to pause
            _paused = true;
        }

        /// <summary>
        /// Jump directly to the frame with the given number. If the given number 
        /// is not in a valid range (0 to total number of frames) or it is very
        /// near to the current frame, the command is ignored.
        /// </summary>
        /// <returns>Returns whether or not the seeking operation was performed.</returns>
        public bool Seek(long frameNumber)
        {
            // If it is the current frame anyways, just ignore the seek command 
            // (for performance reasons this is the first check)
            if (frameNumber == _currentFrame)
                return false;

            // Is the desired frame number in the valid range?
            if (frameNumber < 0 || frameNumber >= RecordingLength)
                return false;

            // Should we do a seek, or are we to close to the current frame?
            if (frameNumber >= _currentFrame - notSeekingScope && 
                frameNumber <= _currentFrame + notSeekingScope)
                return false;

            // Find the offset in the binary stream to skip to
            _seekOffset = frameNumber * SizeOfFrame;

            // Signal the working thread to perform a seek
            _seeking = true;

            // Signal the working thread to perform the seek even if paused,
            // But not unpausing it (_paused flag is left untouched)
            pauseEvent.Set();

            return true;
        }

        private void WriteElapsedMilliseconds(BinaryWriter bw)
        {
            long milliseconds = 0;
            int millisecs = 0;

            // Get the number of elapsed milliseconds since the last recorded frame
            if (timeElapsed.HasValue)
                milliseconds = (DateTime.Now - timeElapsed.Value).Ticks / 10000;

            // Assure the number is in a meaningful range
            if (milliseconds < 0 || milliseconds > Int32.MaxValue)
                millisecs = 0;
            else
                millisecs = (int)milliseconds;

            // Write it into the stream
            bw.Write(millisecs);

            // Remember time for next frame
            timeElapsed = DateTime.Now;
        }

        private void ResetElapsedMilliseconds(BinaryWriter bw)
        {
            // Reset to default
            timeElapsed = null;
        }

        private int ReadElapsedMilliseconds(BinaryReader br)
        {
            // Read in the elapsed milliseconds
            return br.ReadInt32();
        }

        /// <summary>
        /// Closes the underlying stream and releases any resources 
        /// (such as file handles) associated with this recorder instance.
        /// </summary>
        public void Close()
        {
            savefile.Close();
        }

        /// <summary>
        /// Releases all resources used by this recorder instance.
        /// </summary>
        public void Dispose()
        {
            savefile.Dispose();
        }
    }
}
