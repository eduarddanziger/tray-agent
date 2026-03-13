using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Ed.Logging.Log
{
    public class ObservableCappedQueueLogTarget : IObservableLogTarget
    {
        #region LogTarget ("push") Implementation

        public void Write(string newItem)
        {
            if (_minLineLength > 0 && newItem.Length < _minLineLength)
                _cappedQueueBuffer.PushBack(newItem.PadRight(_minLineLength));
            else
                _cappedQueueBuffer.PushBack(newItem);

            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
        }

        #endregion

        #region Privates

        private readonly CappedQueueBuffer<string> _cappedQueueBuffer;

        private readonly int _minLineLength;

        #endregion

        #region Public Defaults Ctor Constants

        /// <summary> The log capped queue buffer capacity </summary>
        /// <remarks> Should be configurable</remarks>
        public const int LogCappedQueueBufferCapacity = 300;

        /// <summary>
        ///     Minimum Length of the log line (in order to stabilize the horizontal scroller's behaviour) or
        ///     zero (switch stabilization off)
        /// </summary>
        /// <remarks> Should be configurable</remarks>
        public const int LogMinLineLength = 200;

        #endregion

        #region Public Ctor

        /// <inheritdoc />
        /// <summary> Ctor with default values, see constants </summary>
        public ObservableCappedQueueLogTarget()
            : this(LogCappedQueueBufferCapacity, LogMinLineLength)
        {
        }

        /// <summary> Ctor </summary>
        /// <param name="capacity"> The capacity to put it to the underline capped queue buffer ctor  . </param>
        /// <param name="minLineLength"> Length of the minimum line or 0 </param>
        public ObservableCappedQueueLogTarget(int capacity, int minLineLength = 0)
        {
            if (minLineLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minLineLength));

            _minLineLength = minLineLength;
            _cappedQueueBuffer = new CappedQueueBuffer<string>(capacity);

            // initial line
            Write(L.LogChannel.Transform("INFO Window logging started"));
        }

        #endregion

        #region Observable Collection Implementation

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IEnumerator<string> GetEnumerator()
        {
            return _cappedQueueBuffer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}