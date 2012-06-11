using System;
using System.Linq;
using Microsoft.Win32;
using RDotNet;
using System.Collections.Generic;

namespace BodyOrientationLib
{
    public class SupervisedLearner : IDisposable
    {
        private static REngine _engine = null;
        private static List<bool> _disposedLearners = new List<bool>();
        private static int _engines = 0;

        // Virtual engine number. -1, if it was initialized as an already learned 
        // instance (no virtual engine needed).
        private int _engineNum = -1;

        private double[][] regressorslist;
        private double[][] responseslist;

        private int count = 0;
        private int numberLearningSamples = 0;

        private double[][] coefficients;
        private bool learned;
        public bool IsLearned { get { return learned; } }

        /// <summary>
        /// Creates an untrained learner, that uses the default number of frames to learn.
        /// </summary>
        public SupervisedLearner() : this(1000) { } 

        /// <summary>
        /// Creates an untrained learner, that uses the given number of frames to learn.
        /// <param name="numberFrames">Number of frames after which this learner switches 
        /// to 'learned' state and predicts instead of further fitting the model.</param>
        /// </summary>
        public SupervisedLearner(int numberFrames)
        {
            // Initialize R engine for the untrained (or to-be-trained) learner
            if (_engine == null)
            {
                #region Find R DLL
#if UNIX
                // Hope, the RDotNet.NativeLibrary project finds it...
#else
                // Try to find the path from the Windows registry and add it to a special environment variable,
                // as well as the subfolders containing the dlls to the PATH variable.
                string rhome = System.Environment.GetEnvironmentVariable("R_HOME");
                if (string.IsNullOrEmpty(rhome)){
                    rhome = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\R-core\R", "InstallPath", "C:");

                    System.Environment.SetEnvironmentVariable("R_HOME", rhome);
                    System.Environment.SetEnvironmentVariable("PATH", System.Environment.GetEnvironmentVariable("PATH") + ";" + 
                                                                        rhome + @"\bin\i386;" + 
                                                                        rhome + @"\bin\x64;");
                }

                // Obsolete: set directory explicitly
                //REngine.SetDllDirectory(rhome + @"\bin\i386");
#endif
                #endregion

                _engine = REngine.CreateInstance("R");
                _engine.InitializeConsoleOutput();
            }

            // Only keep one single instance of the R engine (R.Net restriction: one engine per process).
            // Therefore, make use of different virtual engines via an incrementing number, that is 
            // prepended to each used variable.
            // Also, keep track of all SupervisedLearner instances: if the last one is disposed, no
            // virtual engine is needed anymore: therefore dispose the R engine.
            this._engineNum = _engines;
            _engines++;
            _disposedLearners.Add(false);
            if (_disposedLearners.Count != _engines)
            {
                throw new Exception("Something has gone wrong: the list that keeps track of the virtual engines " +
                    "is different to the number of virtual engines.");
            }

            this.numberLearningSamples = numberFrames;
            this.learned = false;
        }

        /// <summary>
        /// Creates an already trained learner instance, with the given coefficients.
        /// </summary>
        public SupervisedLearner(double[][] coefficients)
        {
            this.coefficients = coefficients;
            this.learned = true;
        }

        public void Dispose()
        {
            // Dispose the virtual engine belonging to this instance
            if (this._engineNum >= 0 && this._engineNum < _disposedLearners.Count)
                _disposedLearners[this._engineNum] = true;

            // If this was the last virtual engine that was left (all are disposed), 
            // dispose the whole R engine
            if (_disposedLearners.All(x => x) && _engine != null)
                _engine.Dispose();
        }

        
        /// <summary>
        /// Feeds regressor values into this learner instance. If it is in 'not learned' 
        /// state, it uses the given regressors and response to save and collect them
        /// until the number of samples is reached. They are then used to fit the model and
        /// learn. The instance then switches into 'learned' state. This method then returns
        /// the predicted response value to the given regressor values.
        /// </summary>
        /// <param name="response">The known response value to fit the model, if in 'not learned'. 
        /// This parameter is ignored in 'learned' state.</param>
        /// <param name="regressors">The regressor values to fit the model.</param>
        /// <returns>The predicted response value, if in 'learned' state. In all other cases, 
        /// the response value given in the parameters is returned.</returns>
        public double[] FeedValues(double[] responses, params double[] regressors)
        {
            if (!learned)
            {
                // Create the storage for learning values
                if (regressorslist == null)
                {
                    count = 0;

                    coefficients = new double[responses.Length][];

                    regressorslist = new double[regressors.Length][];
                    for (int i = 0; i < regressors.Length; i++)
                        regressorslist[i] = new double[numberLearningSamples];

                    responseslist = new double[responses.Length][];
                    for (int i = 0; i < responses.Length; i++)
                        responseslist[i] = new double[numberLearningSamples];
                }

                // Store current learning values
                if (count < numberLearningSamples)
                {
                    for (int i = 0; i < regressors.Length; i++)
                        regressorslist[i][count] = regressors[i];
                    for (int i = 0; i < responses.Length; i++)
                        responseslist[i][count] = responses[i];
                }

                // All values collected -> do model fitting
                else if (count == numberLearningSamples)
                {
                    for (int i = 0; i < regressors.Length; i++)
                        _engine.SetSymbol("r" + _engineNum.ToString() + "_" + i.ToString(), _engine.CreateNumericVector(regressorslist[i]));

                    for (int i = 0; i < responses.Length; i++)
                    {
                        _engine.SetSymbol("y" + _engineNum.ToString() + "_" + i.ToString(), _engine.CreateNumericVector(responseslist[i]));

                        var formula = "fm" + _engineNum.ToString() + "_" + i.ToString() + " <- lm(y" + _engineNum.ToString() + "_" + i.ToString() + " ~ " +
                                        string.Join(" + ", Enumerable.Range(0, regressors.Length).Select(j => "r" + _engineNum.ToString() + "_" + j.ToString())) + ")";

                        //TODO: clean up! this is just a mess of quick-and-dirty lines to trial-and-error some things
                        var output = _engine.EvaluateVerbose(formula);

                        var output2 = _engine.EvaluateVerbose("fm" + _engineNum.ToString() + "_" + i.ToString());
                        var output3 = _engine.EvaluateVerbose("reducedm" + _engineNum.ToString() + "_" + i.ToString() + " <- step(fm" + _engineNum.ToString() + "_" + i.ToString() + ", direction=\"backward\")");
                        var output4 = _engine.EvaluateVerbose("summary(reducedm" + _engineNum.ToString() + "_" + i.ToString() + ")").Item1.AsList();

                        //var x1 = _engine.EagerEvaluate("coef(fm" + _engineNum.ToString() + ")").AsNumeric().ToArray();
                        //var x2 = _engine.EagerEvaluate("resid(fm" + _engineNum.ToString() + ")").AsNumeric().ToArray();
                        //var x3 = _engine.EagerEvaluate("fitted(fm" + _engineNum.ToString() + ")").AsNumeric().ToArray();
                        //Console.WriteLine("Coeffi: {0}\nResidu: {1}\nFitted: {2}",
                        //    string.Join(", ", x1.Select(x => x.ToString("0.00"))),
                        //    string.Join(", ", x2.Select(x => x.ToString("0.00"))),
                        //    string.Join(", ", x3.Select(x => x.ToString("0.00"))));

                        this.coefficients[i] = _engine.Evaluate("coef(fm" + _engineNum.ToString() + "_" + i.ToString() + ")").AsNumeric().ToArray();
                        this.learned = true;
                    }
                }
                count++;
                return responses;
            }
            else
            {
                double[] predictions = new double[responses.Length];
                for (int i = 0; i < responses.Length; i++)
                {
                    double predict = coefficients[i][0];
                    for (int j = 0; j < regressors.Length; j++)
                        predict += regressors[j] * coefficients[i][j + 1];
                    predictions[i] = predict;
                }
                return predictions;
            }
        }
    }
}
