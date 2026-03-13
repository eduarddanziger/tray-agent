using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ed.Logging
{
    /// <summary>Info/Error add INFO or FAIL + class and method names separated by point</summary>
    public class MethodL(
        int totalInfoCount = -1,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
        : IDisposable
    {
        private int _currInfoCount = 0;

        public void Dispose()
        {
            if (totalInfoCount != -1 && _currInfoCount != totalInfoCount)
#pragma warning disable CS0618 // Type or member is obsolete
                if (Marshal.GetExceptionPointers() == IntPtr.Zero && Marshal.GetExceptionCode() == 0)
#pragma warning restore CS0618 // Type or member is obsolete
                    // to be sure we are not in any catch block
                    L.MethodInfo( // ReSharper disable ExplicitCallerInfoArgument
                        memberName, sourceFilePath);
        }

        public void InfoAndConsole(string message)
        {
            Info(message);
            Console.WriteLine(message);
        }

        public void Info(string message)
        {
            if (totalInfoCount != -1)
            {
                ++_currInfoCount;
                L.MethodInfo(message, totalInfoCount, _currInfoCount, memberName, sourceFilePath);
            }
            else
            {
                // ReSharper disable ExplicitCallerInfoArgument
                L.MethodInfo(message, memberName, sourceFilePath);
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
            if (totalInfoCount != -1)
            {
                ++_currInfoCount;
                L.MethodError(message, totalInfoCount, _currInfoCount, memberName, sourceFilePath);
            }
            else
            {
                // ReSharper disable ExplicitCallerInfoArgument
                L.MethodError(message, memberName, sourceFilePath);
                // ReSharper restore ExplicitCallerInfoArgument
            }
        }
    }
}