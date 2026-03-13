using System;

namespace Ed.Logging.Log
{
    /// <summary>
    ///     LogChannel channel class that forwards line on log targets that must subscribe OnForwardingLineToLogTargets
    ///     event
    /// </summary>
    public class LogChannel
    {
        /// <summary> Pre-defined line log type </summary>
        public const int Info = 1;

        /// <summary> Pre-defined error type </summary>
        public const int Error = 2;

        /// <summary>   Forwards line to log targets. You mostly don't need to overrite it</summary>
        public virtual void PushLine(string line, int infoType)
        {
            OnForwardingLineToLogTargets?.Invoke(line, infoType);
        }

        /// <summary> Implements log targets as listeners  </summary>
        public event Action<string, int> OnForwardingLineToLogTargets;

        /// <summary> Shortcut to PushLine(infoText, Info)</summary>
        public void PushInfo(string infoText)
        {
            PushLine(infoText, Info);
        }

        /// <summary> Shortcut to PushLine(infoText, Error)</summary>
        public void PushError(string errorText)
        {
            PushLine(errorText, Error);
        }
    }
}