using System.Collections.Generic;
using System.Collections.Specialized;

namespace Ed.Logging.Log
{
    /// <summary>   Helper interface implementing log target that is an observable string collection </summary>
    // ReSharper disable once InheritdocConsiderUsage
    public interface IObservableLogTarget : INotifyCollectionChanged, IEnumerable<string>, ILogTarget
    {
    }
}