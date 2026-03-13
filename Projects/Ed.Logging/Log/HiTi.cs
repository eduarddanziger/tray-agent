using System;
using System.Diagnostics;

namespace Ed.Logging.Log
{
    /// <summary>   HiTi is a high precision time provider </summary>
    internal class HiTi
    {
        public const int DefaultIdleInSeconds = 30;
        private static HiTi _singletone;
        private readonly TimeSpan _idle;
        private DateTime _startTime;
        private Stopwatch _stopWatch;

        /// <summary>   Ctor with idle time span </summary>
        /// <param name="idleInSeconds">    The idle time span (in seconds) to wait for next sync with high precision Stopwatch . </param>
        public HiTi(int idleInSeconds)
        {
            _idle = TimeSpan.FromSeconds(idleInSeconds);
            Reset();
        }

        public static HiTi Default => _singletone ?? (_singletone = new HiTi(DefaultIdleInSeconds));

        public DateTime UtcNow
        {
            get
            {
                // in _idle time we make a reset
                if (_startTime.Add(_idle) < DateTime.UtcNow)
                    Reset();
                return _startTime.AddTicks(_stopWatch.Elapsed.Ticks);
            }
        }

        public DateTime Now => UtcNow.ToLocalTime();

        private void Reset()
        {
            _startTime = DateTime.UtcNow;
            _stopWatch = Stopwatch.StartNew();
        }
    }
}