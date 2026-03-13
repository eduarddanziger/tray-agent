using System.Collections.Concurrent;

namespace Ed.Logging.Log
{
    public class FileLogTarget : ILogTarget
    {
        #region Default Ctor parameters

        public const string LogFileBaseDirPathname = @"C:\maschine\logs";
        public const string LogFilePrefix = "ed-";
        public const string LogFileExtention = ".log";
        public const int LogFileMaxSizeInBytes = 50000000; // ca 50 MB
        public const int LogFileMaxDaysToKeepFile = 90;
        public const int SleepTimeoutInMs = 10;
        public const int LastChanceTimeoutInMs = 900; // < 1 sec

        #endregion

        #region Implementation objects

        private readonly BackgroundFileWriter _backgroundFileWriter;
        private readonly ConcurrentQueue<string> _concurrentStringQueue;

        #endregion

        #region Public Ctors

        /// <summary>   Default ctor uses private const for all parameters</summary>
        public FileLogTarget()
            : this(
                LogFileMaxSizeInBytes, LogFileBaseDirPathname, LogFilePrefix, LogFileExtention,
                LogFileMaxDaysToKeepFile,
                SleepTimeoutInMs, LastChanceTimeoutInMs)
        {
        }

        /// <summary>   Fully configurable ctor. </summary>
        /// <param name="maxLogFileSizeInBytes">   The maximum log file size in bytes. </param>
        /// <param name="baseDirPathname">         Full pathname of the base directory file. </param>
        /// <param name="filePrefix">              The file prefix. </param>
        /// <param name="fileExtention">           The file extension. </param>
        /// <param name="maxDaysToKeepFile">       The maximum days to keep a log file. </param>
        /// <param name="sleepTimeoutInMs">         Sleep time</param>
        /// <param name="lastChanceTimeoutInMs">    Max timeout to flush messages if cancel sent</param>
        public FileLogTarget(int maxLogFileSizeInBytes, string baseDirPathname, string filePrefix, string fileExtention,
            int maxDaysToKeepFile, int sleepTimeoutInMs, int lastChanceTimeoutInMs)
        {
            _concurrentStringQueue = new ConcurrentQueue<string>();

            _backgroundFileWriter = new BackgroundFileWriter(_concurrentStringQueue,
                maxLogFileSizeInBytes,
                baseDirPathname, filePrefix, fileExtention, maxDaysToKeepFile, sleepTimeoutInMs, lastChanceTimeoutInMs);
        }

        #endregion

        #region Public Interface

        public void Write(string newItem)
        {
            // Worker has problems? just don't filling the queue!
            if (string.IsNullOrEmpty(LastWorkerExceptionAsString))
                _concurrentStringQueue.Enqueue(newItem);
        }

        public void StartBackgroundWorker()
        {
            _backgroundFileWriter.Start();
        }

        public void NotifyWorkerToStop()
        {
            _backgroundFileWriter.NotifyToStop();
        }

        public void WaitForWorkerCompletionInMs(int timeoutInMs)
        {
            _backgroundFileWriter.WaitForCompletionInMs(timeoutInMs);
        }

        /// <summary>   Tells the worker to stop and wits up to timeoutInMs ms for </summary>
        public void NotifyWorkerToStopAndWaitForCompletionInMs(int timeoutInMs)
        {
            _backgroundFileWriter.NotifyToStopAndWaitForCompletionInMs(timeoutInMs);
        }


        public bool WorkerAlive => !_backgroundFileWriter.WorkerLoopCompleted;

        public string LastWorkerExceptionAsString => _backgroundFileWriter.LastExceptionConvertedToString;

        #endregion
    }
}