using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BodyOrientationLib.Fourier;
using System.Numerics;
using System.Globalization;

namespace BodyOrientationLib
{
    public class AnalysisResult
    {
        public double[] CurrentValues { get; set; }
        public double[] Means { get; set; }
        public double[] StandardDeviations { get; set; }
        public double[,] CorrelationMatrix { get; set; }

        public double[] Energies { get; set; }


        public AnalysisResult(int numSequences)
        {
            Means = new double[numSequences];
            StandardDeviations = new double[numSequences];
            CorrelationMatrix = new double[numSequences, numSequences];

            // Fill diagonal of the matrix with ones. Those are never overwritten:
            // The correlation of a sequence with itsself is always one (equal)
            double[,] corrs = CorrelationMatrix;
            for (int i = 0; i < numSequences; i++)
                corrs[i, i] = 1;

            Energies = new double[numSequences];
        }
    }

    public class SequenceAnalyzer
    {
        // Fixed parameter, set in the constructor, for windowsize
        private int _windowsize;
        
        // Pointers for ringbuffers: _startpointer is always 'windowsize' elements behind 
        // _pointer and wraps around to never become negative
        private int _startpointer;
        private int _pointer;
        private int Pointer
        {
            get
            {
                return _pointer;
            }
            set
            {
                if (value != _pointer)
                {
                    _pointer = value % _ringbuffersize;
                    _startpointer = (_ringbuffersize + _pointer - _windowsize + 1) % _ringbuffersize;
                }
            }
        }

        // Size of the ringbuffers is relative to the currently chosen windowsize
        private int _ringbuffersize;

        // Number of buffers is equal to the number of sequences to be analyzed
        private int _numbuffers;

        // Buffers of incomming sequential data
        private double[][] _ringBuffers;

        // Buffers for setting FFT input and getting FFT results
        private Complex[][] _fftBuffers;

        // Store reference to result object; overwrite contents instead of creating 
        // a new object every time for performance reasons
        private AnalysisResult _result;


        public SequenceAnalyzer(int numSequences, int windowSize)
        {
            // Set the basic parameters. Take two times the size of the window for ringbuffers
            _numbuffers = numSequences;
            _windowsize = windowSize;
            _ringbuffersize = windowSize * 2;

            // Set the pointer initially to -1, and therefore the 
            // startpointer to the end of the ringbuffer
            Pointer = -1;

            // Initialize jagged array: one ringbuffer for each sequence
            // each ringbuffer is initialized with its fixed size
            _ringBuffers = new double[_numbuffers][];
            for (int k = 0; k < _numbuffers; k++)
                _ringBuffers[k] = new double[_ringbuffersize];

            // Initialize jagged array: one ring buffer for each sequence
            _fftBuffers = new Complex[_numbuffers][];
            for (int k = 0; k < _numbuffers; k++)
                _fftBuffers[k] = new Complex[_windowsize];

            // Initialize result storage: this object is reused every time.
            // Its contents are overwritten instead of creating a new object everytime
            _result = new AnalysisResult(numSequences);
        }

        /// <summary>
        /// Takes the next values (one for each sequence), calculates and returns the features
        /// of the current window (current value and past windowSize - 1 values).
        /// </summary>
        /// <param name="values">New values, one for each sequence.</param>
        /// <returns>AnalysisResult object containing the results of all calculations.</returns>
        /// <exception cref="ArgumentException">
        /// If the number of given values does not match the number of sequences, set in this 
        /// SequenceAnalyzer object. Also, if one of the values is double.NaN.</exception>
        public AnalysisResult NextValues(params double[] values)
        {
            if (values.Length != _numbuffers)
                throw new ArgumentException("The number of given values does not match the number of " + 
                                            "sequences in this SequenceAnalyzer instance!");

            // Move forward the pointer (wrap around is done in the setter of this property)
            Pointer = Pointer + 1;

            // Write all values in the current pointer position of its respective ringbuffer
            for (int i = 0; i < _numbuffers; i++)
            {
                if (values[i] == double.NaN)
                    throw new ArgumentException("One of the values is double.NaN!");
                _ringBuffers[i][_pointer] = values[i];
            }

            // Calculate Features: they are stored in the AnalysisResult object 'result'
            CalculateTimeStatisticalFeatures();
            CalculateFrequencyFeatures();

            // Store the given values into the result object
            _result.CurrentValues = values;

            // Return the result
            return _result;
        }

        /// <summary>
        /// Calculates the energy of the current window of all sequences of this SequenceAnalyzer 
        /// instance by fourier transforming them into the frequency spectrum. It stores those 
        /// results in the static AnalysisResult object of this instance (_result).
        /// </summary>
        private void CalculateFrequencyFeatures()
        {
            // Fetch array of the AnalysisResult object to write into
            double[] energies = _result.Energies;

            for (int i = 0; i < _numbuffers; i++)
            {
                // Load the current ringbuffer into the buffer of Complex structs with 
                // the size of the window: Since they are just real numbers, set all
                // imaginary parts to zero.
                for (int j = 0; j < _windowsize; j++)
                    _fftBuffers[i][j] = new Complex(_ringBuffers[i][(_startpointer + j) % _ringbuffersize], 0);

                // TODO: take this out again, just here for testing (to copy-paste into the R console)
                var str1 = "signal <- c(" + string.Join(", ", _fftBuffers[i].Select(x => x.Real.ToString("0.000000", new CultureInfo("en-US")))) + ")";

                // Fast Fourier Transform the buffer
                FourierTransform.FFT(_fftBuffers[i], FourierTransform.Direction.Forward);

                // Energy is the sum over squared magnitudes divided by the array length
                // TODO: think about if this is neccessary, or just the periodicity (largest frequency part) is important:
                //energies[i] = _fftBuffers[i].Aggregate(0d, (acc, x) => acc + x.SquaredMagnitude(), acc => acc / _windowsize);

                // Divide the bucket number by the total number of buckets to get the frequency.
                // This way, if we have one timeframe per second, the unit of frequency would be Hz.
                // If we have x timeframes per second, we have to multiply the frequency by this number to get Hz.
                //
                // Lets start by fetching the positive part of the energy density spectrum out of the fourier transformed
                // array. Take only the first half of buckets (the second one is the mirrored negative values. They
                // are symmetrical and therefore not useful for this purpose). Take the squared magnitude of each
                // bucket. Also, skip the first bucket (The DC component) as it doesnt hold any useful information.
                var energyDensity = _fftBuffers[i].Skip(1).Take(_windowsize / 2 - 1).Select(x => x.SquaredMagnitude()).ToArray();

                // TODO: take this out again, just here for testing (to copy-paste into the R console)
                var str2 = "energy <- c(" + string.Join(", ", energyDensity.Select(x => x.ToString("0.000000", new CultureInfo("en-US")))) + ")";

                // Then take the maximum of all buckets, and get the frequency out of the bucket number.
                // Further assuming we are sampling on about 30 frames per second, multiply this number to get the
                // frequency in Herz.
                // TODO: set back factor to 30
                if (i == 0)
                {
                    int inx;
                    
                    inx = energyDensity.IndexOfMax();
                    energies[i] = (double)(inx + 1) / (double)_windowsize * 3;
                    energyDensity[inx] = double.NegativeInfinity;

                    inx = energyDensity.IndexOfMax();
                    energies[i + 1] = (double)(inx + 1) / (double)_windowsize * 3;
                    energyDensity[inx] = double.NegativeInfinity;

                    inx = energyDensity.IndexOfMax();
                    energies[i + 2] = (double)(inx + 1) / (double)_windowsize * 3;
                }
            }
        }

        /// <summary>
        /// Calculates the means and standard deviations of the current window of all 
        /// sequences of this SequenceAnalyzer instance and the matrix of correlation 
        /// coefficients between them. It stores those results in the static AnalysisResult
        /// object of this instance (_result).
        /// </summary>
        private void CalculateTimeStatisticalFeatures()
        {
            // Fetch arrays of the AnalysisResult object to write into
            double[] means = _result.Means;
            double[] sds = _result.StandardDeviations;
            double[,] corrs = _result.CorrelationMatrix;

            // For each ringbuffer, calculate the mean and standard deviation of the current window
            for (int i = 0; i < _numbuffers; i++)
            {
                means[i] = CalculateMean(_ringBuffers[i]);
                sds[i] = CalculateStandardDeviation(_ringBuffers[i], means[i]);
            }

            // For each pair of different sequences calculate the correlation.
            for (int i = 0; i < _numbuffers - 1; i++)
            {
                for (int j = i + 1; j < _numbuffers; j++)
                {
                    corrs[i, j] = CalculateCorrelationCoefficient(_ringBuffers[i], _ringBuffers[j], means[i], means[j], sds[i], sds[j]);

                    // Mirror results on the diagonal of the matrix due to the symmetry of correlation.
                    corrs[j, i] = corrs[i, j];
                }
            }
        }

        /// <summary>
        /// Calculates the mean of the current window in the given ringbuffer. The current 
        /// window is determined by the _startpointer of this SequenceAnalyzer instance and 
        /// a length of _windowsize (wrap around in the ringbuffer).
        /// </summary>
        private double CalculateMean(double[] ringbuffer)
        {
            double mean = 0;

            // Sum up all values of the current window
            for (int i = 0; i < _windowsize; i++)
                mean += ringbuffer[(_startpointer + i) % _ringbuffersize];

            // Divide them by the number of values in the window
            return mean / _ringbuffersize;
        }

        /// <summary>
        /// Calculates the standard deviation. The given mean has to correspond to the 
        /// values of the given ringbuffer. Furthermore only the current window starting 
        /// with _startpointer and a length of _windowsize (wrap around in the ringbuffer) 
        /// is taken out of the buffers to calculate the standard deviation.
        /// </summary>
        private double CalculateStandardDeviation(double[] ringbuffer, double mean)
        {
            double s = 0, diff = 0;

            // Sum over all squared values
            for (int i = 0; i < _windowsize; i++)
            {
                diff = (ringbuffer[(_startpointer + i) % _ringbuffersize] - mean);

                // More efficient to multiply it with itself than to square it via Math.Pow(x, 2)
                s += diff * diff;
            }

            // Divide by number of values minus one (statistical variance) 
            // and calculate the squareroot of it (transform variance to standard deviation)
            return Math.Sqrt(s / (_ringbuffersize - 1));
        }

        /// <summary>
        /// Calculates the correlation coefficient via a numerical stable version of the
        /// pearson algorithm. It is designed for the use with ringbuffers that are used
        /// in this specific instance of SequenceAnalyzer and makes several assumtions about
        /// its input parameters: both ringbuffers have to be exactly _ringbuffersize large,
        /// the given means and standard deviations have to correspond to the values of the
        /// given ringbuffers. Furthermore only the current window starting with _startpointer
        /// and a length of _windowsize (wrap around in the ringbuffer) is taken out of the 
        /// buffers to calculate the correlation coefficient.
        /// </summary>
        private double CalculateCorrelationCoefficient(double[] ringbuffer1, double[] ringbuffer2, double mean1, double mean2, double s1, double s2)
        {
            double r = 0;
            int curIndex = 0;

            // Numerical stable version of correlation coefficient calculation:
            // Sum over all products of values minus its respective mean divided 
            // by its standard deviation.
            // Just slightly slower than the numerical unstable version, because 
            // of the divisions inside the loop.
            for (int i = 0; i < _windowsize; i++)
            {
                curIndex = (_startpointer + i) % _ringbuffersize;
                r += ((ringbuffer1[curIndex] - mean1) / s1) *
                     ((ringbuffer2[curIndex] - mean2) / s2);
            }

            // Finally divide by number of values minus one
            return r / (_ringbuffersize - 1);
        }
    }
}
