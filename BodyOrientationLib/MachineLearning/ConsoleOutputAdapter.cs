using System;
using RDotNet.Internals;
using RDotNet.Devices;
using RDotNet;
using System.Collections.Generic;
using System.Text;

namespace BodyOrientationLib
{
	/// <summary>
	/// The default IO device.
	/// </summary>
	public class ConsoleOutputAdapter : ICharacterDevice
	{
		#region ICharacterDevice Members

        private StringBuilder savedStream;
        private bool saveStream = false;

        public void BeginSaveStream()
        {
            savedStream = new StringBuilder();
            saveStream = true;
        }

        public string EndSaveStream()
        {
            if (saveStream && savedStream != null)
            {
                var result = savedStream.ToString();
                savedStream = null;
                saveStream = false;

                return result;
            }

            return string.Empty;
        }

		public string ReadConsole(string prompt, int capacity, bool history)
		{
			return null;
		}

		public void WriteConsole(string output, int length, ConsoleOutputType outputType)
		{
            if (saveStream)
                savedStream.Append(output);
        }

		public void ShowMessage(string message)
		{}

		public void Busy(BusyType which)
		{}

		public void Callback()
		{}

		public YesNoCancel Ask(string question)
		{
			return default(YesNoCancel);
		}

		public void Suicide(string message)
		{
			CleanUp(StartupSaveAction.Suicide, 2, false);
		}

		public void ResetConsole()
		{}

		public void FlushConsole()
		{}

		public void ClearErrorConsole()
		{}

		public void CleanUp(StartupSaveAction saveAction, int status, bool runLast)
		{
			Environment.Exit(status);
		}

		public bool ShowFiles(string[] files, string[] headers, string title, bool delete, string pager)
		{
			return false;
		}

		public string ChooseFile(bool create)
		{
			return null;
		}

		public void EditFile(string file)
		{}

        public SymbolicExpression LoadHistory(Language call, SymbolicExpression operation, Pairlist args, REnvironment environment)
		{
			return environment.Engine.NilValue;
		}

        public SymbolicExpression SaveHistory(Language call, SymbolicExpression operation, Pairlist args, REnvironment environment)
		{
			return environment.Engine.NilValue;
		}

        public SymbolicExpression AddHistory(Language call, SymbolicExpression operation, Pairlist args, REnvironment environment)
		{
			return environment.Engine.NilValue;
		}

		#endregion
	}

    public static class REngineExtensions
    {
        private static Dictionary<string, ConsoleOutputAdapter> consoleAdapters = new Dictionary<string,ConsoleOutputAdapter>();

        public static void InitializeConsoleOutput(this REngine engine)
        {
            var consoleAdapter = new ConsoleOutputAdapter();

            consoleAdapters.Add(engine.ID, consoleAdapter);
            engine.Initialize(new StartupParameter() { Quiet = false, Slave = false, Verbose = true }, consoleAdapter);
        }

        /// <summary>
        /// Evaluates and returns the resulting symbolic expression together with the string output of the R console in a tuple.
        /// If the evaluation fails, the error is returned in a textual form inside the string part of the tuple. The
        /// symbolic expression object is null in such a case.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="statement">The statement to evaluate</param>
        /// <returns></returns>
        public static Tuple<SymbolicExpression, string> EvaluateVerbose(this REngine engine, string statement)
        {
            if (engine == null)
            {
                throw new ArgumentNullException("engine");
            }
            if (!engine.IsRunning)
            {
                throw new ArgumentException("Engine is not running", "engine");
            }
            if (!consoleAdapters.ContainsKey(engine.ID))
            {
                throw new ArgumentException("Engine was not initialized for console output via the InitializeConsoleOutput method", "engine");
            }

            var consoleAdapter = consoleAdapters[engine.ID];
            SymbolicExpression symb = null;
            var exceptionMsg = string.Empty;

            consoleAdapter.BeginSaveStream();
            try
            {
                symb = engine.Evaluate(statement);
            }
            catch (Exception ex)
            {
                exceptionMsg = "An exception occured while executing the statement \"" + statement + "\":\n" + ex.Message + "\n\n" + ex.StackTrace + "\n\nThe console output while executing was:\n";
            }

            return new Tuple<SymbolicExpression, string>(symb, exceptionMsg + consoleAdapter.EndSaveStream());
        }
    }
}