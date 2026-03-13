using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ed.Logging
{
    /// <summary>Info/Error add INFO or FAIL + class and method names separated by point</summary>
    public class MethodL : IDisposable
    {
        private readonly string _memberName;
        private readonly string _sourceFilePath;
        private readonly int _totalInfoCount;
        private int _currInfoCount;

        public MethodL(int totalInfoCount = -1, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "")
        {
            _totalInfoCount = totalInfoCount;
            _memberName = memberName;
            _sourceFilePath = sourceFilePath;
            _currInfoCount = 0;
        }

        public void Dispose()
        {
            if (_totalInfoCount != -1 && _currInfoCount != _totalInfoCount)
#pragma warning disable CS0618 // Type or member is obsolete
                if (Marshal.GetExceptionPointers() == IntPtr.Zero && Marshal.GetExceptionCode() == 0)
#pragma warning restore CS0618 // Type or member is obsolete
                    // to be sure we are not in any catch block
                    L.MethodInfo( // ReSharper disable ExplicitCallerInfoArgument
                        _memberName, _sourceFilePath);
        }

        public void InfoAndConsole(string message)
        {
            Info(message);
            Console.WriteLine(message);
        }

        public void Info(string message)
        {
            if (_totalInfoCount != -1)
            {
                ++_currInfoCount;
                L.MethodInfo(message, _totalInfoCount, _currInfoCount, _memberName, _sourceFilePath);
            }
            else
            {
                // ReSharper disable ExplicitCallerInfoArgument
                L.MethodInfo(message, _memberName, _sourceFilePath);
                // ReSharper restore ExplicitCallerInfoArgument
            }
        }

        public void ErrorAndConsole(string line)
        {
            Error(line);
            Console.WriteLine(line);
        }

        public void Error(string message)
        {
            if (_totalInfoCount != -1)
            {
                ++_currInfoCount;
                L.MethodError(message, _totalInfoCount, _currInfoCount, _memberName, _sourceFilePath);
            }
            else
            {
                // ReSharper disable ExplicitCallerInfoArgument
                L.MethodError(message, _memberName, _sourceFilePath);
                // ReSharper restore ExplicitCallerInfoArgument
            }
        }
    }
}