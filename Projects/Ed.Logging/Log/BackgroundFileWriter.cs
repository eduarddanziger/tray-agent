using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;

namespace Ed.Logging.Log
{
    internal class BackgroundFileWriter
    {
        #region Public Ctor

        /// <summary>
        ///     Ctor defines a worker loop that writes queue to rotating files.
        ///     Ctor DOES NOT START WORKER, see start function
        /// </summary>
        /// <remarks>   Any exception is caught but breaks loop and cancels worker.  </remarks>
        /// <param name="concurrentStringQueue">    The queue to be written</param>
        /// <param name="maxLogFileSizeInBytes">    The maximum log file size in bytes. Then rotation</param>
        /// <param name="baseDirPathname">          Full pathname of the folder with log files. </param>
        /// <param name="filePrefix">               The log file prefix. </param>
        /// <param name="fileExtention">            The log file extension. </param>
        /// <param name="maxDaysToKeepFile">        The maximum days to keep file. The older files are REMOVED</param>
        /// <param name="sleepTimeIntervalInMs">    Sleep time</param>
        /// <param name="lastChanceTimeoutInMs">    Max timeout to flush messages if cancel sent</param>
        public BackgroundFileWriter(ConcurrentQueue<string> concurrentStringQueue, int maxLogFileSizeInBytes,
            string baseDirPathname, string filePrefix, string fileExtention, int maxDaysToKeepFile,
            int sleepTimeIntervalInMs,
            int lastChanceTimeoutInMs)
        {
            _lastChanceTimeoutInMs = lastChanceTimeoutInMs;
            _baseDirPathname = baseDirPathname;

            _maxLogFileSizeInBytes = maxLogFileSizeInBytes;
            _filePrefix = filePrefix;
            _fileExtention = fileExtention;
            _maxDaysToKeepFile = maxDaysToKeepFile;
            _sleepTimeIntervalInMs = sleepTimeIntervalInMs;


            _concurrentStringQueue = concurrentStringQueue;

            LastExceptionConvertedToString = string.Empty;

            // set up background worker
            _backgroundWorker = new BackgroundWorker {WorkerSupportsCancellation = true};


            // Nothing is started here! It's a definition only
            _backgroundWorker.DoWork +=
                (sender, args) => WritingLoopIncludingFileRotatingAndRemovalOfOld(sender as BackgroundWorker, args);
        }

        /// <summary>   Writing loop including file rotating and old file removal. </summary>
        private void WritingLoopIncludingFileRotatingAndRemovalOfOld(BackgroundWorker backgroundWorker,
            // ReSharper disable once SuggestBaseTypeForParameter
            DoWorkEventArgs args)
        {
            try
            {
                using (
                    var rotatingFileOutput = new RotatingFileOutput(_baseDirPathname, _filePrefix, _fileExtention,
                        _maxLogFileSizeInBytes))
                {
                    WriteIntroducingLine(rotatingFileOutput);

                    for (;;)
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        if (backgroundWorker.CancellationPending)
                        {
                            WriteAllPendingLinesWithinTimeout(rotatingFileOutput);
                            break; // break the thread loop!
                        }

                        // Something to do? Then do it! Otherwise do "lazy" things 
                        if (WriteNextPendingLineIfAny(rotatingFileOutput))
                        {
                            // OK there was something written; probably something else comes..
                            Thread.Sleep(_sleepTimeIntervalInMs); // TODO: Change all to auto reset event
                        }
                        else
                        {
                            // "lazy" things if nothing was written
                            Thread.Sleep(_sleepTimeIntervalInMs);
                            if (rotatingFileOutput.FlushIfNeeded())
                            {
                                Thread.Sleep(_sleepTimeIntervalInMs);
                            }
                            else
                            {
                                // even more "lazy" things if there was nothing to flush
                                Thread.Sleep(_sleepTimeIntervalInMs);
                                rotatingFileOutput.DeleteNextFileOlderThenInDays(_maxDaysToKeepFile);
                                Thread.Sleep(_sleepTimeIntervalInMs); // one sleep more
                            }

                            Thread.Sleep(_sleepTimeIntervalInMs); // one sleep more
                        }
                    }

                    WriteFinalLineAndFlush(rotatingFileOutput);
                }
            }
            catch (Exception exception)
            {
                LastExceptionConvertedToString = exception.ToString();
            }
            finally
            {
                args.Cancel = true; // logically canceled.. correct?
                WorkerLoopCompleted = true;
            }
        }

        /// <summary>
        ///     Put "introducing line". normally at the point where the logging starts
        ///     (normally in the very beginning of application)
        /// </summary>
        private static void WriteIntroducingLine(RotatingFileOutput rotatingFileOutput)
        {
            rotatingFileOutput.PutLineAndRotateFileIfNeeded(
                L.LogChannel.Transform("INFO BackgroundFileWriter: File logging started."));
        }

        /// <summary> Put "final line". normally at the point where (and if) the logging is gracefully finished</summary>
        private static void WriteFinalLineAndFlush(RotatingFileOutput rotatingFileOutput)
        {
            rotatingFileOutput.PutLineAndRotateFileIfNeeded(
                L.LogChannel.Transform("INFO BackgroundFileWriter: File logging finished."));
            rotatingFileOutput.FlushIfNeeded(); // last flush!
        }

        #endregion

        #region Private Helpers

        /// <summary>   Writes a next line. </summary>
        /// <returns>   true if there was smth to be written. </returns>
        private bool WriteNextPendingLineIfAny(RotatingFileOutput rotatingFileOutput)
        {
            string nextLine;
            if (_concurrentStringQueue.TryDequeue(out nextLine))
            {
                rotatingFileOutput.PutLineAndRotateFileIfNeeded(nextLine);

                return true;
            }

            return false;
        }

        /// <summary>   Writes all pending lines. </summary>
        private void WriteAllPendingLinesWithinTimeout(RotatingFileOutput rotatingFileOutput)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(_lastChanceTimeoutInMs))
            {
                string nextLine;
                if (!_concurrentStringQueue.TryDequeue(out nextLine))
                    break;
                rotatingFileOutput.PutLineAndRotateFileIfNeeded(nextLine, true);
            }
        }

        #endregion

        #region Private Members

        private readonly string _baseDirPathname;
        private readonly ConcurrentQueue<string> _concurrentStringQueue;

        private readonly int _maxLogFileSizeInBytes;
        private readonly string _filePrefix;
        private readonly string _fileExtention;
        private readonly int _maxDaysToKeepFile;
        private readonly int _sleepTimeIntervalInMs;
        private readonly int _lastChanceTimeoutInMs;

        private string _lastExceptionConvertedToString;
        private readonly object _lastExceptionConvertedToStringLock = new object();

        private bool _workerLoopCompleted;
        private readonly object _workerLoopCompletedLock = new object();

        private readonly BackgroundWorker _backgroundWorker;

        #endregion

        #region Public Manipulators (start/stop etc.)

        /// <summary>   Starts the worker. </summary>
        public void Start()
        {
            _backgroundWorker.RunWorkerAsync();
        }

        /// <summary>   Tells worker he should stop. </summary>
        public void NotifyToStop()
        {
            _backgroundWorker.CancelAsync();
        }

        public void WaitForCompletionInMs(int timeoutInMs)
        {
            // give a chance to asynchronous socket worker to close everything etc.
            for (var iMsCounter = 0; iMsCounter < timeoutInMs; iMsCounter += _sleepTimeIntervalInMs)
            {
                if (WorkerLoopCompleted)
                    break;
                Thread.Sleep(_sleepTimeIntervalInMs);
            }
        }

        /// <summary>   Tells the worker to stop and waits up to timeoutInMs ms for </summary>
        public void NotifyToStopAndWaitForCompletionInMs(int timeoutInMs)
        {
            NotifyToStop();

            WaitForCompletionInMs(timeoutInMs);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Last exception converted to string.
        ///     IF NOT NULL AND NOT EMPTY, THEN THE WORKER IS NOT OK!
        /// </summary>
        public string LastExceptionConvertedToString
        {
            get
            {
                lock (_lastExceptionConvertedToStringLock)
                {
                    return _lastExceptionConvertedToString;
                }
            }
            private set
            {
                lock (_lastExceptionConvertedToStringLock)
                {
                    _lastExceptionConvertedToString = value;
                }
            }
        }

        /// <summary>   WorkerLoopCompleted is introduced because the native RunWorkerCompleted event isn't fired</summary>
        public bool WorkerLoopCompleted
        {
            get
            {
                lock (_workerLoopCompletedLock)
                {
                    return _workerLoopCompleted;
                }
            }
            private set
            {
                lock (_workerLoopCompletedLock)
                {
                    _workerLoopCompleted = value;
                }
            }
        }

        #endregion
    }
}