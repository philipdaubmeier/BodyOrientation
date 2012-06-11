using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace BodyOrientationLib
{
    public abstract class Multiplexer<TOut>
    {
        public class StreamInfo
        {
            public string Name { get; set; }
            public int StreamId { get; set; }
            public long TimeOffsetMilliseconds { get; set; }
            public int NumValues { get; set; }

            /// <summary>
            /// Set by the multiplexer base class.
            /// </summary>
            public int CurrentIndex { get; set; }
        }

        protected class BufferEntry
        {
            public long Timestamp;
            public IMultiplexable Item;
            public double[] Values;
        }

        public enum InterpolationMethod
        {
            None = 1,
            Linear = 2,
            Square = 3,
            Cubic = 4
        }

        public event EventHandler<ItemMultiplexedEventArgs<TOut>> ItemMultiplexed;

        private InterpolationMethod interpolationMethod;

        private BufferEntry[][] ringBuffers;
        private StreamInfo[] streamMetadata;
        
        private int numStreams;
        private int windowSize;

        private int[] streamIdMapping;
        private int firstStreamId;

        public Multiplexer(InterpolationMethod interpolationMethod, params StreamInfo[] streamMetadata) 
        {
            if(streamMetadata == null)
                throw new ArgumentNullException("streamMetadata");

            if(streamMetadata.Length == 0)
                throw new ArgumentException("Not a single stream info given for streamMetadata!");

            this.interpolationMethod = interpolationMethod;

            // Store metadata
            this.numStreams = streamMetadata.Length;
            this.streamMetadata = streamMetadata;

            // Fill the mapping from stream id to ringbuffer index.
            // This is later on needed if new values are pushed into.
            // Uses buckets (array) instead of a dictionary for performance 
            // and due to stream Ids being very small (it can be assumed <10).
            //
            // Checks if any stream id was set more than once. Any stream Id that
            // is not mapped gets mapped to -1.
            bool first = true;
            this.streamIdMapping = new int[streamMetadata.Max(x => x.StreamId) + 1];
            for (int i = 0; i < streamIdMapping.Length; i++)
            {
                switch (streamMetadata.Count(x => x.StreamId == i))
                {
                    case 0: { streamIdMapping[i] = -1; break; }
                    case 1:
                        {
                            streamIdMapping[i] = streamMetadata.IndexWhere(x => x.StreamId == i);
                            if (first) { firstStreamId = i; first = false; }
                            break;
                        }
                    default: { throw new ArgumentException("Stream id '" + i + "' was set more than once!"); }
                }
            }
            if (streamMetadata.Any(x => x.StreamId < 0))
                throw new ArgumentException("Stream ids have to be greater or equal to 0!");

            // Initialize ringbuffers: one for each ingoing stream and each one as long 
            // as the interpolation method needs (plus a bit of additional room)
            windowSize = (int)interpolationMethod + 3;
            this.ringBuffers = new BufferEntry[streamMetadata.Length][];
            for (int i = 0; i < streamMetadata.Length; i++)
            {
                this.ringBuffers[i] = new BufferEntry[windowSize];
                for (int j = 0; j < ringBuffers[i].Length; j++)
                    ringBuffers[i][j] = new BufferEntry() { Timestamp = -1 };
            }
        }

        protected void Push(int streamId, IMultiplexable item)
        {
            int streamIndex = 0;
            if (streamId < 0 || streamId >= streamIdMapping.Length ||
                    (streamIndex = streamIdMapping[streamId]) == -1)
                throw new ArgumentException("Invalid stream id!");

            StreamInfo streamInfo = streamMetadata[streamIndex];

            // Fetch the next ringbuffer entry to put this new item into
            int newIndex = (streamInfo.CurrentIndex + 1) % windowSize;
            streamInfo.CurrentIndex = newIndex;
            BufferEntry newEntry = ringBuffers[streamIndex][newIndex];

            newEntry.Timestamp = DateTime.Now.Ticks + streamInfo.TimeOffsetMilliseconds;
            newEntry.Item = item;

            // Do we have to interpolate?
            if (interpolationMethod != InterpolationMethod.None)
            {
                // Extract values to interpolate
                double[] values = item.ExtractValues(), prevValues = null, prev2Values = null, prev3Values = null;
                long timestamp = newEntry.Timestamp, prevTimestamp = 0, prev2Timestamp = 0, prev3Timestamp = 0;

                if (values == null)
                    throw new ArgumentNullException("Could not extract any values from given item!");

                if (streamInfo.NumValues != values.Length)
                    throw new ArgumentException("Length of extracted value array does not match the length stored in the metadata!");

                // Clone the given item, before it gets modified
                newEntry.Item = (IMultiplexable)item.Clone();

                // Fetch values from past items for interpolation.
                switch (interpolationMethod)
                {
                    case InterpolationMethod.Cubic:
                        {
                            prev3Values = ringBuffers[streamIndex][(streamInfo.CurrentIndex + windowSize - 3) % windowSize].Values;
                            prev3Timestamp = ringBuffers[streamIndex][(streamInfo.CurrentIndex + windowSize - 3) % windowSize].Timestamp;
                            goto case InterpolationMethod.Square;
                        }
                    case InterpolationMethod.Square:
                        {
                            prev2Values = ringBuffers[streamIndex][(streamInfo.CurrentIndex + windowSize - 2) % windowSize].Values;
                            prev2Timestamp = ringBuffers[streamIndex][(streamInfo.CurrentIndex + windowSize - 2) % windowSize].Timestamp;
                            goto case InterpolationMethod.Linear;
                        }
                    case InterpolationMethod.Linear:
                        {
                            prevValues = ringBuffers[streamIndex][(streamInfo.CurrentIndex + windowSize - 1) % windowSize].Values;
                            prevTimestamp = ringBuffers[streamIndex][(streamInfo.CurrentIndex + windowSize - 1) % windowSize].Timestamp;
                            break;
                        }
                }

                // Interpolate every single value
                if (interpolationMethod == InterpolationMethod.Linear && prevValues != null)
                    for (int i = 0; i < values.Length; i++) { }
                        //values[i] = InterpolateLinear(prevValues[i], values[i]);
                else if (interpolationMethod == InterpolationMethod.Square && prevValues != null && prev2Values != null)
                    for (int i = 0; i < values.Length; i++) { }
                        //values[i] = InterpolateSquare(prev2Values[i], prevValues[i], values[i]);
                else if (interpolationMethod == InterpolationMethod.Cubic && prevValues != null && prev2Values != null && prev3Values != null)
                    for (int i = 0; i < values.Length; i++) { }
                        //values[i] = InterpolateCubic(prev3Values[i], prev2Values[i], prevValues[i], values[i]);

                // Store the interpolated values in the buffer for interpolating the next items
                newEntry.Values = values;

                // Inject the interpolated values back into the actual item, that is given back 
                newEntry.Item.InjectValues(values);
            }

            // Multiplex (Pack all items of a time frame into one,
            // if a new value was pushed from the first stream
            if (streamId == firstStreamId)
            {
                // Pack the values into an item (implemented by a subclass of this abstract class)
                // and sends out an event that a new item is ready
                IMultiplexable[] toPack = new IMultiplexable[streamMetadata.Length];
                for (int i = 0; i < streamMetadata.Length; i++)
                    toPack[i] = ringBuffers[i][streamMetadata[i].CurrentIndex].Item;

                OnItemMultiplexed(PackMultiplexItem(toPack));
            }
        }

        protected abstract TOut PackMultiplexItem(IMultiplexable[] values);

        protected void OnItemMultiplexed(TOut item)
        {
            if (ItemMultiplexed != null && Application.Current != null)
                Application.Current.Dispatcher.BeginInvoke(ItemMultiplexed, this, 
                            new ItemMultiplexedEventArgs<TOut>() { MultiplexedItem = item });
        }
    }

    public class ItemMultiplexedEventArgs<T> : EventArgs
    {
        public T MultiplexedItem { get; set; }
    }

    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Finds the first index in the collection where the predicate evaluates to true.
        /// Returns -1 if no matching item found
        /// </summary>
        /// <typeparam name="TSource">Type of collection</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="predicate">Function to evaluate</param>
        /// <returns>Index where predicate is true, or -1 if not found.</returns>
        public static int IndexWhere<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            var enumerator = source.GetEnumerator();
            int index = 0;
            while (enumerator.MoveNext())
            {
                TSource obj = enumerator.Current;
                if (predicate(obj))
                    return index;
                index++;
            }
            return -1;
        }
    }
}
