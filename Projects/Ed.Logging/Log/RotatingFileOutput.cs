using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ed.Logging.Log
{
    /// <summary>   File output tool that rotates file (switches output to the next one) if it reaches a predefined length </summary>
    internal sealed class RotatingFileOutput : IDisposable
    {
        #region Public Ctor

        /// <summary> Constructor. Throws only DEFENSIVE exceptions, does nothing more!</summary>
        /// <remarks> File is NOT INITIALIZED here, see <see cref="InitOrSwitchToNextFile" /></remarks>
        public RotatingFileOutput(string baseDirPathname, string filePrefix, string fileExtension,
            int maxFileSizeInBytes)
        {
            if (string.IsNullOrEmpty(baseDirPathname) || string.IsNullOrEmpty(filePrefix))
                throw new ArgumentException("Base directory or file prefix can be neither null nor empty");

            if (string.IsNullOrEmpty(fileExtension) || fileExtension[0] != '.')
                throw new ArgumentException("File extension must begin with \".\"");

            _baseDirPathname = baseDirPathname;
            _filePrefix = filePrefix;
            _fileExtension = fileExtension;

            _maxFileSizeInBytes = maxFileSizeInBytes;

            CharNumberWrittenToCurrentFile = 0;
        }

        #endregion

        #region Public Members

        public string PathnameOfCurrentFile { get; private set; }

        #endregion

        #region Private Properties

        private int CharNumberWrittenToCurrentFile { get; set; }

        #endregion

        #region Public Disposable Implementation

        public void Dispose()
        {
            Close();
        }

        #endregion

        #region File Name Time Format Consts

        private const string FileNameDateTemplate = "yyyyMMdd";
        private const string FileNameTimeTemplate = "HHmmss";
        private const string FileNameDateTimeFormat = FileNameDateTemplate + "-" + FileNameTimeTemplate;

        #endregion

        #region File Header Format Consts

        private const string HeaderVersionLineFormat = "### Application: {0}; Version: {1}";
        private readonly string _headerBorderLine = new string('#', 96);

        #endregion

        #region Private Members

        private FileStream _currentFileStream;
        private bool _needFlush;
        private StreamWriter _streamWriter;
        private readonly string _baseDirPathname;
        private readonly string _fileExtension;
        private readonly string _filePrefix;
        private readonly int _maxFileSizeInBytes;

        #endregion

        #region Public Interface

        /// <summary>   Flushes file to disk if needed. </summary>
        /// <returns>   true if smth was flushed. </returns>
        public bool FlushIfNeeded()
        {
            if (_needFlush)
            {
                _streamWriter.Flush();
                _needFlush = false;
                return true;
            }

            return false;
        }

        public void Close()
        {
            if (null != _currentFileStream)
            {
                _streamWriter.Close();
                _streamWriter = null;

                _currentFileStream.Close();
                _currentFileStream = null;
            }
        }

        /// <summary>   Deletes the next file elder then in days described by maxDayNumber. </summary>
        /// <remarks> Does not read the file date but parses the file name!</remarks>
        public void DeleteNextFileOlderThenInDays(int maxDayNumber)
        {
            var name = Directory.EnumerateFiles(_baseDirPathname, _filePrefix + "*" + _fileExtension)
                .Where(s =>
                {
                    var fileNameOnly = Path.GetFileNameWithoutExtension(s);
                    var expectedFileNameLength = _filePrefix.Length + FileNameDateTimeFormat.Length;
                    // our files have long names!

                    if (string.IsNullOrWhiteSpace(fileNameOnly)
                        || fileNameOnly.Length < expectedFileNameLength // then it's NOT OUR file
                        ||
                        !DateTime.TryParseExact(fileNameOnly.Substring(_filePrefix.Length, FileNameDateTemplate.Length),
                            FileNameDateTemplate, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dateTime))
                        return false;

                    return DateTime.Now.Subtract(dateTime).TotalDays > maxDayNumber;
                })
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(name))
                try
                {
                    File.Delete(name);
                }
                catch
                {
                    // Failed delete is ignored; We'll just try to delete it next time
                }
        }

        public void PutLineAndRotateFileIfNeeded(string nextLine, bool preventFileRotation = false)
        {
            if (null == nextLine)
                throw new ArgumentNullException(nameof(nextLine));

            // open or switch (rotate) file if too much chars
            if (string.IsNullOrEmpty(PathnameOfCurrentFile))
            {
                PathnameOfCurrentFile = InitOrSwitchToNextFile();
            }
            else if (!preventFileRotation && CharNumberWrittenToCurrentFile > _maxFileSizeInBytes)
            {
                PathnameOfCurrentFile = InitOrSwitchToNextFile();
                CharNumberWrittenToCurrentFile = 0;
            }

            PutLineUnchecked(nextLine);

            // increment char counter
            CharNumberWrittenToCurrentFile += nextLine.Length;
        }

        #endregion

        #region Private Utility Functions

        /// <summary>   Puts a next line to the underlying stream without any check. </summary>
        private void PutLineUnchecked(string line)
        {
            Debug.Assert(null != _currentFileStream, "File stream was not initiated");

            _streamWriter.WriteLine(line);

            _needFlush = true;
        }

        /// <summary>   Prepares a new output file or close and initializes the next one (file rotation). </summary>
        /// <returns>   file name </returns>
        private string InitOrSwitchToNextFile()
        {
            // Get next name
            var fileNameWithoutExtension = _filePrefix + DateTime.Now.ToString(FileNameDateTimeFormat);
            var filePathname = Path.Combine(_baseDirPathname, fileNameWithoutExtension + _fileExtension);

            // If directory exists, it's possible that the file already exists! Then we increment 3d (and/or 2nd) digit after comma
            if (!Directory.Exists(_baseDirPathname))
                Directory.CreateDirectory(_baseDirPathname);

            // Close previous
            Close();

            // Open next file
            _currentFileStream = File.Open(filePathname, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);

            _streamWriter = new StreamWriter(_currentFileStream);

            // Put header info
            var appInfoTitle = "Unknown";
            var appInfoVersion = "Unknown";
            var entryAssembly = Assembly.GetEntryAssembly();

            if (null != entryAssembly)
            {
                appInfoTitle = QueryAssemblyTitle(entryAssembly);
                appInfoVersion = QueryAssemblyVersion(entryAssembly).ToString();
            }

            _streamWriter.WriteLine(_headerBorderLine);
            _streamWriter.WriteLine(HeaderVersionLineFormat, appInfoTitle, appInfoVersion);
            _streamWriter.WriteLine(_headerBorderLine);
            _needFlush = true;

            return filePathname;
        }

        private static string QueryAssemblyTitle(Assembly assembly)
        {
            var attributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            return attributes.FirstOrDefault() is AssemblyTitleAttribute titleAttribute
                ? titleAttribute.Title
                : assembly.GetName().Name;
        }

        private static Version QueryAssemblyVersion(Assembly assembly)
        {
            return assembly.GetName().Version;
        }

        #endregion
    }
}