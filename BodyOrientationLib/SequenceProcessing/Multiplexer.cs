using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace BodyOrientationLib
{
    /// <summary>
    /// Implements a generic and very versatile base class for concrete multiplexers.
    /// The main logic of multiplexing is implemented here. The Multiplexer essentially takes
    /// multiple time series of input objects (objects of classes that implement the 
    /// IMultiplexable interface, and have a set of double values, so they can be interpolated)
    /// and packs them into a single stream of output objects.
    /// 
    /// The multiplexer can then be used like follows: Create an instance of the multiplexer,
    /// subscribe to the ItemMultiplexed event, and call the PushXValue methods every time a new 
    /// item on a stream arrives. These PushXValues must be implemented for a concrete Multiplexer
    /// and should call the generic Push method of the base class (for details, see the example 
    /// on the bottom).
    ///
    /// <example>
    /// <code>
    /// var multiplexer = new TupleMultiplexer();
    ///
    /// multiplexer.ItemMultiplexed += (s, e) => { Console.WriteLine(e.MultiplexedItem); };
    ///
    /// multiplexer.PushIntegerValue(42);
    /// multiplexer.PushDoubleValue(Math.PI);
    /// </code>
    /// </example>
    /// 
    /// The multiplexer demonstated in the above example is using a very simple implementation
    /// of the generic abstract Multiplexer. To make this example work, a small class NumberWrapper
    /// was designed, as only IMultiplexable objects can be multiplexed. It has only a single 
    /// property and has implicit cast operators to allow to cast between the actual numbers and
    /// the wrapper:
    /// 
    /// <example>
    /// <code>
    /// public class TupleMultiplexer : Multiplexer&lt;Tuple&lt;int, double&gt;&gt;
    /// {
    ///     public class NumberWrapper : IMultiplexable
    ///     {
    ///         public double TheValue { get; set; }
    /// 
    ///         public int NumMultiplexableItems { get { return 1; } }
    ///         public double[] ExtractValues() { return new double[] { TheValue }; }
    ///         public void InjectValues(double[] values) { TheValue = values[0]; }
    ///         public object Clone() { return new NumberWrapper() { TheValue = this.TheValue }; }
    /// 
    ///         public static implicit operator NumberWrapper(int value) { return new NumberWrapper() { TheValue = value }; }
    ///         public static implicit operator NumberWrapper(double value) { return new NumberWrapper() { TheValue = value }; }
    ///         public static implicit operator int(NumberWrapper value) { return (int)value.TheValue; }
    ///         public static implicit operator double(NumberWrapper value) { return value.TheValue; }
    ///     }
    /// 
    ///     public TupleMultiplexer()
    ///         : base(InterpolationMethod.None, new StreamInfo[]{
    ///             new StreamInfo(){
    ///                 StreamId = 0,
    ///                 Name = "Integer stream",
    ///                 NumValues = 1
    ///             },
    ///             new StreamInfo(){
    ///                 StreamId = 1,
    ///                 Name = "Floating point number stream",
    ///                 NumValues = 1
    ///             }
    ///     }) { }
    /// 
    ///     public void PushIntegerValue(int value) { base.Push(0, (NumberWrapper)value); }
    ///     public void PushDoubleValue(double value) { base.Push(1, (NumberWrapper)value); }
    /// 
    ///     protected override Tuple&lt;int, double&gt; PackMultiplexItem(IMultiplexable[] values)
    ///     {
    ///         return new Tuple&lt;int, double&gt;((int)(NumberWrapper)values[0], (double)(NumberWrapper)values[1]);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>Author: Philip Daubmeier</remarks>
    /// </summary>
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

            if (interpolationMethod != InterpolationMethod.None)
                throw new NotImplementedException("Interpolation is not implemented yet!");

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

                // TODO: where is the point in time where we want our interpolation result?
                // For testing, hardcoded to be 30 ms in the past.
                long timestampWanted = timestamp - 30;

                // Interpolate every single value
                if (interpolationMethod == InterpolationMethod.Linear && prevValues != null)
                    for (int i = 0; i < values.Length; i++)
                        values[i] = InterpolateLinear(prevTimestamp, prevValues[i], timestamp, values[i], timestampWanted);
                else if (interpolationMethod == InterpolationMethod.Square && prevValues != null && prev2Values != null)
                    for (int i = 0; i < values.Length; i++)
                        values[i] = InterpolateSquare(prev2Timestamp, prev2Values[i], prevTimestamp, prevValues[i], timestamp, values[i], timestampWanted);
                else if (interpolationMethod == InterpolationMethod.Cubic && prevValues != null && prev2Values != null && prev3Values != null)
                    for (int i = 0; i < values.Length; i++)
                        values[i] = InterpolateCubic(prev3Timestamp, prev3Values[i], prev2Timestamp, prev2Values[i], prevTimestamp, prevValues[i], timestamp, values[i], timestampWanted);

                // TODO: handle special case where it should be interpolated between two angles:
                // we need to annotate the properties that represent angles in the featuresets.
                // Also include the kind of angle in this annotation: degrees or radians? -pi to pi
                // or 0 to 2*pi range? Do interpolation then via the LerpAngle/LerpRadians method, 
                // already implemented in another module of this project.
                // Also interpolate Quaternions with the Lerp method, instead interpolating the X,Y,Z,W
                // components seperately.
                // Those open questions, and the performance overhead when interpolating every time
                // is the reason the constructor throws a NotImplementedException for every
                // interpolation method except from 'None'. It has shown, the sensor and kinect input
                // streams are fairly regular and do not need interpolation for multiplexing.
                // This simplifies things a lot!

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

        /// <summary>
        /// Implements a linear interpolation method. The two given points dont have to be equidistant, 
        /// and are given as (x, y) tuples. The last parameter x is the point where the respective f(x) 
        /// is searched for, which is then returned.
        /// </summary>
        private double InterpolateLinear(double x0, double y0, double x1, double y1, double x)
        {
            return y0 * (x - x1) / (x0 - x1) + y1 * (x - x0) / (x1 - x0);
        }

        /// <summary>
        /// Implements Nevilles method to interpolate with a quadratic polynom. Complexity is O(n²).
        /// Points dont have to be equidistant, and are given as (x, y) tuples. The last parameter
        /// x is the point where the respective f(x) is searched for, which is then returned.
        /// </summary>
        private double InterpolateSquare(double x0, double y0, double x1, double y1, double x2, double y2, double x)
        {
            return NevillesInterpolator(new double[] { x0, x1, x2 }, new double[] { y0, y1, y2 }, x);
        }

        /// <summary>
        /// Implements Nevilles method to interpolate with a cubic polynom. Complexity is O(n²).
        /// Points dont have to be equidistant, and are given as (x, y) tuples. The last parameter
        /// x is the point where the respective f(x) is searched for, which is then returned.
        /// </summary>
        private double InterpolateCubic(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3, double x)
        {
            return NevillesInterpolator(new double[] { x0, x1, x2, x3 }, new double[] { y0, y1, y2, y3 }, x);
        }

        /// <summary>
        /// Implements Nevilles method to interpolate with polynoms of (n-1)-th grade between n
        /// given points. Points dont have to be equidistant, and are given as (x, y) tuples. The 
        /// last parameter x is the point where the respective f(x) is searched for, which is 
        /// then returned.
        /// </summary>
        private double NevillesInterpolator(double[] xi, double[] f, double x)
        {
            int n = xi.Length - 1;
            for (int m = 1; m <= n; m++)
                for (int i = 0; i <= n - m; i++)
                    f[i] = ((x - xi[i + m]) * f[i] + (xi[i] - x) * f[i + 1]) / (xi[i] - xi[i + m]);

            return f[0];
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
